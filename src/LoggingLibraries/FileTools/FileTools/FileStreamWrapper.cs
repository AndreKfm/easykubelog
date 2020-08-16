using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileToolsClasses
{

    public class FileStreamWrapper : IFileStream, IFileStreamWriter
    {
        FileStream _stream;
        FileStreamReader _reader;
        FileSeeker _seeker = new FileSeeker();

        public FileStreamWrapper(string path, FileMode mode, FileAccess access, FileShare share)
        {
            _stream = new FileStream(path, mode, access, share);
        }

        public FileStreamWrapper(FileStream assignStreamToThisClass)
        {
            _stream = assignStreamToThisClass;
        }

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

        public int Read(Span<byte> buffer)
        {
            return _stream.Read(buffer);
        }

        public int Read(byte[] buffer)
        {
            return _stream.Read(buffer);
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
            return _seeker.SeekLastLineFromCurrentAndPositionOnStartOfIt(this);
        }

        public IFileStreamReader GetStreamReader()
        {
            if (_reader == null)
                _reader = new FileStreamReader(_stream);
            return _reader;
        }

        public void Write(Span<byte> buffer)
        {
            _stream.Write(buffer);
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

        public string ReadLineFromCurrentPositionToEnd(long maxStringSize)
        {
            var result = InternalReadLineFromCurrentPositionToEnd(maxStringSize);
            if (String.IsNullOrEmpty(result))
            {
                if (_stream != null)
                {
                    _stream.Seek(_currentPosition, SeekOrigin.Begin);
                }
            }
            return result;
        }

        byte[] _localBuffer; // We hold the buffer in a local variable for reuse - since we don't
                             // want to have the GC to do that much


        public string InternalReadLineFromCurrentPositionToEnd(long maxStringSize)
        {
            try
            {
                if (_stream == null)
                {
                    _stream = new FileStreamWrapper(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    _stream.Seek(0, SeekOrigin.End);
                    _currentPosition = _stream.Position;

                    // Seek back to the last line - if we don't do that we will miss the first written line

                    var foundLine = _stream.SeekLastLineFromCurrentAndPositionOnStartOfIt();
                    if (!foundLine)
                        return String.Empty; // There is no line feed - that is by definition wrong - so let's what has been written
                    _currentPosition = _stream.Position;
                }
                else
                {
                    _stream.Seek(_currentPosition, SeekOrigin.Begin);
                }

                long current = _stream.Position;
                long maxToRead = _stream.Length - current;
                long toRead = maxToRead;
                if (toRead > maxStringSize)
                {
                    toRead = maxStringSize;
                }
                if (toRead <= 0)
                    return String.Empty;

                if ((_localBuffer == null) || (_localBuffer.Length != toRead))
                {
                    _localBuffer = new byte[toRead];
                    Console.WriteLine($"Realloc: [{toRead}]");
                }

                var buffer = _localBuffer;
                var read = _stream.Read(buffer);

                var lastIndex = Array.LastIndexOf<byte>(buffer, (byte)'\n');

                if (lastIndex < 0)
                {
                    return String.Empty;
                }

                ++lastIndex; // Return also the \n character
                string result = System.Text.Encoding.Default.GetString(buffer, 0, lastIndex);
                if (String.IsNullOrEmpty(result) == false)
                    _currentPosition = _stream.Position;

                if (lastIndex < buffer.Length)
                {
                    // We couldn't read a complete line at the end - so position to the last index
                    long diff = buffer.Length - lastIndex;
                    _currentPosition -= diff;
                    if (_currentPosition < 0)
                    {
                        // That shouldn't happen ?!
                        _currentPosition = 0;
                    }

                }

                return result;
            }
            catch (Exception e)
            {
                Console.Error.Write(e.Message);
                return String.Empty;
            }
        }



    }


}
