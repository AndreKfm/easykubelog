using DirectoryWatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WatcherFileListClasses
{
    public class GetFileWrapper : IGetFile
    {
        public IFile GetFile(string fileName)
        {
            return new FileReadOnlyWrapper(fileName);
        }
    }

    public class NewOutput
    {
        public NewOutput(string lines, string filename, string lastError)
        {
            Lines = lines;
            Filename = filename;
            LastError = lastError;
        }

        public readonly string LastError;
        public readonly string Lines;
        public readonly string Filename;
    }

    public interface IAutoCurrentFileList : IDisposable
    {
        public void Start(string directoryToWatch, IFileSystemWatcher watcherInterface = null, int updateRatioInMilliseconds = 0);
        public void Stop();
        public Task BlockingReadAsyncNewOutput(Action<NewOutput, CancellationToken> callback); // Stop() will abort read
    }

    public class AutoCurrentFileList : IAutoCurrentFileList
    {
        WatcherFileList _watcher;
        WatcherCurrentFileList fileList;
        string _directoryToWatch;
        readonly IGetFile _getFile;
        Task _current;
        public AutoCurrentFileList(IGetFile openFile = null)
        {
            _getFile = openFile ?? new GetFileWrapper();
        }

        public void Stop()
        {
            _source?.Cancel();
            _watcher?.Dispose();
            _watcher = null;
            _channel?.Writer.Complete();
            _channelNewOutput?.Writer.Complete();
            _current?.Wait();
            return;
        }

        public void Start(string directoryToWatch, IFileSystemWatcher watcherInterface = null, int updateRatioInMilliseconds = 0)
        {
            Stop();
            _directoryToWatch = directoryToWatch;
            _watcher = new WatcherFileList(directoryToWatch, watcherInterface, updateRatioInMilliseconds);
            _source = new CancellationTokenSource(); ;
            fileList = new WatcherCurrentFileList();

            _channel = Channel.CreateBounded<FileTask>(MaxFileChanges);
            _channelNewOutput = Channel.CreateBounded<NewOutput>(MaxFileChanges);

            _current = Task.Run(async () => await this.ReadChannel(_source.Token));
            _watcher.Start((list) =>
            {
                HandleFileChanges(list);
            });
        }

        public enum FileTaskEnum
        {
            Add, Remove, Update
        }

        public class FileTask
        {
            public FileTask(string fileName, FileTaskEnum fileTask, string lastError)
            {
                this.FileName = fileName;
                this.FileOperation = fileTask;
                this.LastError = lastError;
            }

            public readonly string FileName;
            public readonly FileTaskEnum FileOperation;
            public readonly string LastError;
        }

        const int MaxFileChanges = 16384;
        Channel<FileTask> _channel;
        Channel<NewOutput> _channelNewOutput;

        private void Error(string error)
        {
            Console.Error.WriteLine(error);
            _lastError = error;
        }

        string _lastError = String.Empty;

        private void WriteToChannel(FileEntry change, FileTaskEnum operation)
        {
            if (!_channel.Writer.TryWrite(new FileTask(change.FileName, operation, _lastError)))
            {
                Error($"HandleFileChanges - Channel full: {change.FileName} {operation}");
            }
            else
            {
                if (_lastError != String.Empty)
                    _lastError = String.Empty;
            }
        }

        private void HandleFileChanges(ReadOnlyCollection<FileEntry> changes)
        {
            foreach (var entry in changes)
            {
                var c = entry.LastChanges;
                if (c.HasFlag(IFileSystemWatcherChangeType.Created)) WriteToChannel(entry, FileTaskEnum.Add);
                if (c.HasFlag(IFileSystemWatcherChangeType.Changed)) WriteToChannel(entry, FileTaskEnum.Update);
                if (c.HasFlag(IFileSystemWatcherChangeType.Rename)) WriteToChannel(entry, FileTaskEnum.Remove);
                if (c.HasFlag(IFileSystemWatcherChangeType.Error)) WriteToChannel(entry, FileTaskEnum.Remove);
                if (c.HasFlag(IFileSystemWatcherChangeType.Deleted)) WriteToChannel(entry, FileTaskEnum.Remove);
            }
        }

        private async ValueTask<NewOutput> ReadAsyncNewOutput() // Stop() will abort read
        {
            return await _channelNewOutput.Reader.ReadAsync(_source.Token);
        }

        public enum ReadAsyncOperation
        {
            StopRead, ContinueRead
        }
        public async Task BlockingReadAsyncNewOutput(Action<NewOutput, CancellationToken> callback) // Stop() will abort read
        {
            var token = _source.Token;
            while (!token.IsCancellationRequested)
            {
                var newOutput = await ReadAsyncNewOutput();
                callback(newOutput, _source.Token);
                //if (callback(newOutput, _source.Token) != ReadAsyncOperation.ContinueRead)
                //  break; // Cancelled by external callee
            }
        }

        private async Task ReadChannel(CancellationToken token)
        {

            try
            {
                while (token.IsCancellationRequested == false)
                {
                    var op = await _channel.Reader.ReadAsync(token);
                    switch (op.FileOperation)
                    {
                        case FileTaskEnum.Add:
                            {
                                AddFile(op);
                                Console.WriteLine($"### ADD {op.FileName}");
                                break;
                            }
                        case FileTaskEnum.Remove:
                            {
                                fileList.RemoveFile(op.FileName);
                                Console.WriteLine($"### REMOVE {op.FileName}");
                                break;
                            }
                        case FileTaskEnum.Update:
                            {
                                var list = fileList.GetList();
                                IFile file = null;
                                if (!list.TryGetValue(op.FileName, out CurrentFileEntry value))
                                {
                                    file = AddFile(op);
                                }
                                else
                                {
                                    file = value.CurrentFile;
                                }

                                string content = file.ReadLineFromCurrentPositionToEnd();
                                Console.WriteLine($"### UpDATE {op.FileName}  {content}");
                                if (!_channelNewOutput.Writer.TryWrite(new NewOutput(content, op.FileName, op.LastError)))
                                {
                                    Error($"AutoCurrentFileList.ReadChannel error - write to callback for new outputs failed: {op.FileName}:prev error:{op.LastError}:{content}");
                                }

                                break;
                            }
                    }
                }
            }
            catch (Exception) { }
        }

        private IFile AddFile(FileTask op)
        {
            var fileStream = _getFile.GetFile(Path.Combine(_directoryToWatch, op.FileName));
            fileList.AddFile(new CurrentFileEntry(op.FileName, fileStream));
            return fileStream;
        }

        public void Dispose()
        {
            Stop();
        }

        CancellationTokenSource _source = new CancellationTokenSource();
    }
}
