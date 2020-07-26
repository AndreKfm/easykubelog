using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata.Ecma335;

namespace WatcherFileListClasses
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


    public class WatcherCurrentFileList : ICurrentFileList
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


    public interface IFileStream : IDisposable
    {
        long Seek(long offset, SeekOrigin origin);
        long Position { get; set; }
        long Length { get; }
        int Read(byte[] buffer);
    }

    public class FileStreamWrapper : IFileStream
    {
        public FileStreamWrapper(string path, FileMode mode, FileAccess access, FileShare share)
        {
            _stream = new FileStream(path, mode, access, share);
        }

        FileStream _stream;

        public long Position { get => _stream.Position; set => _stream.Position = value; }

        public long Length => _stream.Length;

        public long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }

        public int Read(byte[] buffer)
        {
            return _stream.Read(buffer);
        }
    }

    public class FileReadOnlyWrapper : IFile
    {
        long currentPosition = 0;
        readonly string _fileName;
        IFileStream _stream;
        public FileReadOnlyWrapper(string fileName, IFileStream stream = null)
        {
            _fileName = fileName;
            _stream = stream; // Can be passed for mocking - otherwise it will be generated internally by a wrapper implementation
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
                       _stream = new FileStreamWrapper(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
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

                ++lastIndex; // Return also the \n character
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

}
