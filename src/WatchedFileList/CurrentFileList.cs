using DirectoryWatching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Enumeration;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WatchedFileList
{
    
    // Used to wrap single filestream 
    public interface IFileReadonlyWrapper : IDisposable
    {

    }

    public interface IOpenFile
    {
        IFileReadonlyWrapper OpenForRead(string fileName);
    }
    
    
    public class CurrentFileEntry
    {
        
        public CurrentFileEntry(string fileName, IFileReadonlyWrapper fileStream)
        {
            FileName = fileName;
            CurrentFile = fileStream;
        }
        public string FileName { get; private set; } // Current file name

        public IFileReadonlyWrapper CurrentFile { get; private set; } // Current file name
    }

    public interface ICurrentFileList
    {
        bool AddFile(CurrentFileEntry currentFileEntry); // Returns false if the file exists or fileName is null
    }


    public class CurrentFileList : ICurrentFileList
    {

        public bool AddFile(CurrentFileEntry currentFileEntry)
        {
            var old = fileList;
            fileList = fileList.Add(currentFileEntry.FileName, currentFileEntry);
            return (old != fileList);
        }

        public bool RemoveFile(string fileName)
        {
            var old = fileList;
            if (!fileList.ContainsKey(fileName)) 
                return false;
            fileList[fileName].CurrentFile?.Dispose();
            fileList = fileList.Remove(fileName);
            return (old != fileList);
        }


        public ImmutableDictionary<string /** fileName */, CurrentFileEntry> GetList()
        {
            return fileList;
        }

        ImmutableDictionary<string /** fileName */, CurrentFileEntry> fileList = ImmutableDictionary<string, CurrentFileEntry>.Empty;
    }

    public class FileReadOnlyWrapper : IFileReadonlyWrapper
    {
        readonly FileStream _stream;
        public FileReadOnlyWrapper(FileStream stream)
        {
            _stream = stream;
        }

        public void Dispose()
        {
            _stream?.Close();
            _stream?.Dispose();
        }
    }


    public class OpenFileWrapper : IOpenFile
    {
        public IFileReadonlyWrapper OpenForRead(string fileName)
        {
            try
            {
                var fileStream = File.OpenRead(fileName);
                return new FileReadOnlyWrapper(fileStream);
            }
            catch(Exception)
            {

            }

            return null; // Empty object on any error
        }
    }

    public class AutoCurrentFileList
    {
        WatchFileList _watcher;
        CurrentFileList fileList;
        string _directoryToWatch;
        IOpenFile _openFile; 
        public AutoCurrentFileList(IOpenFile openFile = null)
        {
            _openFile = openFile ?? new OpenFileWrapper();
        }

        public void Start(string directoryToWatch, IFileSystemWatcher watcherInterface = null, int updateRatioInMilliseconds = 0)
        {
            _source?.Cancel();
            _watcher?.Dispose();
            _directoryToWatch = directoryToWatch;
            _watcher = new WatchFileList(directoryToWatch, watcherInterface, updateRatioInMilliseconds);
            _source = new CancellationTokenSource(); ;
            fileList = new CurrentFileList();
            
            channel = Channel.CreateBounded<FileTask>(MaxFileChanges);
            Task.Run(async () => await this.ReadChannel(_source.Token));
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
            public FileTask(string fileName, FileTaskEnum fileTask)
            {
                this.fileName = fileName;
                this.fileTask = fileTask;
            }

            public readonly string fileName;
            public readonly FileTaskEnum fileTask;
        }

        const int MaxFileChanges = 16384;
        Channel<FileTask> channel;

        private void HandleFileChanges(ReadOnlyCollection<FileEntry> changes)
        {
            foreach (var entry in changes)
            {

                

                var c = entry.LastChanges;
                if (c.HasFlag(IFileSystemWatcherChangeType.Created)) channel.Writer.TryWrite(new FileTask(entry.FileName, FileTaskEnum.Add));
                if (c.HasFlag(IFileSystemWatcherChangeType.Changed)) channel.Writer.TryWrite(new FileTask(entry.FileName, FileTaskEnum.Update));
                if (c.HasFlag(IFileSystemWatcherChangeType.Rename)) channel.Writer.TryWrite(new FileTask(entry.FileName, FileTaskEnum.Remove));
                if (c.HasFlag(IFileSystemWatcherChangeType.Error)) channel.Writer.TryWrite(new FileTask(entry.FileName, FileTaskEnum.Remove));
                if (c.HasFlag(IFileSystemWatcherChangeType.Deleted)) channel.Writer.TryWrite(new FileTask(entry.FileName, FileTaskEnum.Remove));
            }
        }


        private async Task ReadChannel(CancellationToken token)
        {

            try
            {
                while (token.IsCancellationRequested == false)
                {
                    var op = await channel.Reader.ReadAsync(token);
                    switch(op.fileTask)
                    {
                        case FileTaskEnum.Add:
                            {
                                var fileStream = _openFile.OpenForRead(Path.Combine(_directoryToWatch, op.fileName));
                                fileList.AddFile(new CurrentFileEntry(op.fileName, fileStream));
                                Console.WriteLine($"### ADD {op.fileName}");
                                break;
                            }
                        case FileTaskEnum.Remove:
                            {
                                fileList.RemoveFile(op.fileName);
                                Console.WriteLine($"### REMOVE {op.fileName}");
                                break;
                            }
                        case FileTaskEnum.Update:
                            {
                                Console.WriteLine($"### UpDATE {op.fileName}");
                                // TODO READ FILE CONTENT
                                break;
                            }
                    }
                }
            }
            catch (Exception) { }
        }

        CancellationTokenSource _source = new CancellationTokenSource();
    }
}
