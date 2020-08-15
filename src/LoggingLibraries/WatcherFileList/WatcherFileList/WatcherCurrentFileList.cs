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

    public interface IFileStreamReader
    {
        string ReadLine();
    }

    public interface IFileStream : IDisposable
    {
        long Seek(long offset, SeekOrigin origin);
        long Position { get; set; }
        long Length { get; }
        int Read(byte[] buffer);

        IFileStreamReader GetStreamReader();
        public bool SeekLastLineFromCurrentAndPositionOnStartOfIt();

    }

    public class FileStreamReader : IFileStreamReader
    {
        StreamReader _reader;
        public FileStreamReader(FileStream stream)
        {
            _reader = new StreamReader(stream);
        }

        public string ReadLine()
        {
            return _reader.ReadLine();
        }

        enum WhichPosition
        {
            OneBeforeIfNotStartOfFile, OneBehindIfNotStartOfFile
        }
        
        


    }

    public class FileStreamWrapper : IFileStream
    {
        public FileStreamWrapper(string path, FileMode mode, FileAccess access, FileShare share)
        {
            _stream = new FileStream(path, mode, access, share);
        }

        FileStream _stream;
        FileStreamReader _reader;

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


        bool SeekNextLineFeedInNegativeDirectionAndPositionStreamOnIt(int steps)
        {
            Span<byte> buffer = new byte[steps];
            var initial = _stream.Position;
            for (; ; )
            {
                var current = _stream.Position;
                if (current == 0)
                {
                    break;
                }
                int toRead = steps;
                if (toRead > current)
                {
                    toRead = (int)current;
                    buffer = buffer.Slice(0, steps);
                }
                SetPositionRelative(-toRead);
                var currendMidPos0 = _stream.Position;
                int size = _stream.Read(buffer);
                var currentMidPos = _stream.Position;
                if (size != steps)
                {
                    // That shouldn't happen ???
                    break;
                }

                int index = buffer.LastIndexOf((byte)'\n');
                if (index >= 0)
                {
                    var posBefore = _stream.Position;
                    var newPos = toRead - index;
                    SetPositionRelative(-newPos);
                    var pos = _stream.Position;
                    return true;
                }
                SetPositionRelative(-toRead); // Continue with next characters
            }

            SetPosition(initial);
            return false;

        }

        bool SetPositionRelative(long offset)
        {
            var current = _stream.Position;
            var newPos = _stream.Seek(offset, SeekOrigin.Current);
            var current2 = _stream.Position;
            return (newPos - current) == offset; // We assume that we won't position more than Int32.Max
        }

        void SetPosition(long position)
        {
            _stream.Seek(position, SeekOrigin.Begin);
        }

        public bool SeekLastLineFromCurrentAndPositionOnStartOfIt()
        {
            int steps = 80;

            var pos1 = _stream.Position;

            var found1 = SeekNextLineFeedInNegativeDirectionAndPositionStreamOnIt(steps);
            if (found1 == false)
                return false; // No line feed found - so no line yet
            var pos2 = _stream.Position;

            var found2 = SeekNextLineFeedInNegativeDirectionAndPositionStreamOnIt(steps);

            var pos3 = _stream.Position;

            if (found2)
            {
                // Ok we found a second linefeed - so one character after will be the start of our line
                SetPositionRelative(1);
                var pos4 = _stream.Position;
                return true;
            }

            // We found one LF but not another one - so there is only one line 
            // -> we can read this line if we position to the begin of the file
            SetPosition(0);
            return true;
        }

        public IFileStreamReader GetStreamReader()
        {
            if (_reader != null)
                return _reader;
            _reader = new FileStreamReader(_stream);
            return _reader;
        }
    }

    public class FileReadOnlyWrapper : IFile
    {
        long _currentPosition = 0;
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
                    _stream.Seek(0, SeekOrigin.End);
                    _currentPosition = _stream.Position;

                    // Seek back to the last line - if we don't do that we will miss the first written line

                    bool foundLine = _stream.SeekLastLineFromCurrentAndPositionOnStartOfIt();
                    if (!foundLine)
                        return String.Empty; // There is no line feed - that is by definition wrong - so let's what has been written
                }
                else _stream.Seek(_currentPosition, SeekOrigin.Begin);

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

                _currentPosition = _stream.Position;
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
