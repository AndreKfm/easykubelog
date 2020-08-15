using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileToolsClasses
{

    public class FileStreamWrapper : IFileStream, IFileStreamWriter
    {
        public FileStreamWrapper(string path, FileMode mode, FileAccess access, FileShare share)
        {
            _stream = new FileStream(path, mode, access, share);
        }

        public FileStreamWrapper(FileStream assignStreamToThisClass)
        {
            _stream = assignStreamToThisClass;
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

        public int Read(Span<byte> buffer)
        {
            return _stream.Read(buffer);
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

        public string ReadLineFromCurrentPositionToEnd(long maxStringSize = 65536 * 4)
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
                    toRead = maxStringSize;
                if (toRead <= 0)
                    return String.Empty;

                if ((_localBuffer == null) || (_localBuffer.Length != toRead))
                {
                    _localBuffer = new byte[toRead];
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
