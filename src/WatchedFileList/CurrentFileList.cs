using DirectoryWatching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WatchedFileList
{

    public interface IGetFile
    {
        public IFile GetFile(string fileName);
    }

    public interface IFile
    {
        public string ReadLineFromCurrentPositionToEnd(long maxStringSize = 16384); // Read all data as string from current position to the last occurence
                                                                                    // of '\n'. If '\n' is not found the whole string will be returned if maxStringSize
                                                                                    // has been reached - otherwise an empty string will be returned and more data
                                                                                    // on the next call if '\n' is found
    }


    public class CurrentFileEntry
    {
        
        public CurrentFileEntry(string fileName, IFile fileStream)
        {
            FileName = fileName;
            CurrentFile = fileStream;
        }
        public string FileName { get; private set; } // Current file name

        public IFile CurrentFile { get; private set; } // Current file name
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
            fileList = fileList.Remove(fileName);
            return (old != fileList);
        }


        public ImmutableDictionary<string /** fileName */, CurrentFileEntry> GetList()
        {
            return fileList;
        }

        ImmutableDictionary<string /** fileName */, CurrentFileEntry> fileList = ImmutableDictionary<string, CurrentFileEntry>.Empty;
    }

    public class FileReadOnlyWrapper : IFile
    {
        long currentPosition = 0;
        string _fileName;
        FileStream _stream;
        public FileReadOnlyWrapper(string fileName)
        {
            _fileName = fileName;
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }


        public string ReadLineFromCurrentPositionToEnd(long maxStringSize = 16384)
        {
            try
            {
                if (_stream == null)
                {
                       _stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                }


                _stream.Seek(currentPosition, SeekOrigin.Begin);

                long current = _stream.Position;
                long maxToRead = _stream.Length - current;
                long toRead = maxToRead;
                if (toRead > maxStringSize)
                    toRead = maxStringSize;
                if (toRead <= 0)
                    return String.Empty;

                var buffer = new byte[toRead];
                var read = _stream.Read(buffer);
                
                var lastIndex = Array.LastIndexOf<byte>(buffer, (byte)'\n');
                
                if (lastIndex < 0)
                {
                    return String.Empty;
                }

                string result = System.Text.Encoding.Default.GetString(buffer, 0, lastIndex);

                currentPosition = _stream.Position;
                return result;
            }
            catch (Exception e)
            {
                Console.Error.Write(e.Message);
                return String.Empty;
            }
        }



    }


    //public class OpenFileWrapper : IOpenFile
    //{
    //    public IFileReadonlyWrapper OpenForRead(string fileName)
    //    {
    //        try
    //        {
    //            var fileStream = File.OpenRead(fileName);
    //            return new FileReadOnlyWrapper(fileStream);
    //        }
    //        catch(Exception)
    //        {

    //        }

    //        return null; // Empty object on any error
    //    }
    //}

    public class GetFileWrapper : IGetFile
    {
        public IFile GetFile(string fileName)
        {
            return new FileReadOnlyWrapper(fileName);
        }
    }

    public class AutoCurrentFileList
    {
        WatchFileList _watcher;
        CurrentFileList fileList;
        string _directoryToWatch;
        IGetFile _getFile;
        public AutoCurrentFileList(IGetFile openFile = null)
        {
            _getFile = openFile ?? new GetFileWrapper();
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
                                AddFile(op);
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
                                var list = fileList.GetList();
                                IFile file = null;
                                if (!list.TryGetValue(op.fileName, out CurrentFileEntry value))
                                {
                                    file = AddFile(op);
                                }
                                else
                                {
                                    file = value.CurrentFile;
                                }

                                string content = file.ReadLineFromCurrentPositionToEnd();
                                Console.WriteLine($"### UpDATE {op.fileName}  {content}");

                                break;
                            }
                    }
                }
            }
            catch (Exception) { }
        }

        private IFile AddFile(FileTask op)
        {
            var fileStream = _getFile.GetFile(Path.Combine(_directoryToWatch, op.fileName));
            fileList.AddFile(new CurrentFileEntry(op.fileName, fileStream));
            return fileStream;
        }

        CancellationTokenSource _source = new CancellationTokenSource();
    }
}
