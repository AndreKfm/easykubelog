using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
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

        byte[] _localBuffer; // We hold the buffer in a local variable for reuse - since we don't
                             // want to have the GC to do that much

        int _reallocCounter = 0;
        int ReallocAfterXCountsLowerThan50Percent = 20; // If a smaller buffer would have reallocated for 20 times - then assume
                                                        // we have allocated a really big one and free it again

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



        // Will be used for unit testing only - since Moq cannot handle Span<byte>
        public string FOR_UNIT_TEST_SLOW_FUNCTION_ReadLineCharByCharTillCRLF()
        {
            if (_stream == null)
                return String.Empty;

            string result = String.Empty;
            byte[] buf = new byte[1];
            int index = 0;
            var pos = _stream.Position; 
            for (; ; )
            {
                if (_stream.Read(buf) != 1)
                    return String.Empty;
                var c = buf[0];

                ++index;
                if (c == '\n')
                    break;
            }

            buf = new byte[index];
            _stream.Seek(pos, SeekOrigin.Begin);
            if (_stream.Read(buf) != index)
                return String.Empty;
            string complete = System.Text.Encoding.Default.GetString(buf);
            return complete;
        }

        public (string line, ReadLine sizeExceeded) ReadLineFromCurrentPositionToEnd(long maxStringSize)
        {
            var result = InternalReadLineFromCurrentPositionToEnd(maxStringSize);
            if (String.IsNullOrEmpty(result.line))
            {
                if (_stream != null)
                {
                    _stream.Seek(_currentPosition, SeekOrigin.Begin);
                }
            }
            return result;
        }
        private bool InitializeNewStream()
        {
            _stream = new FileStreamWrapper(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            _stream.Seek(0, SeekOrigin.End);
            _currentPosition = _stream.Position;

            // Seek back to the last line - if we don't do that we will miss the first written line

            var foundLine = _stream.SeekLastLineFromCurrentAndPositionOnStartOfIt();
            if (!foundLine)
                return false; // There is no line feed - that is by definition wrong - so let's what has been written
            _currentPosition = _stream.Position;
            return true;
        }


        public (string line, ReadLine sizeExceeded) InternalReadLineFromCurrentPositionToEnd(long maxStringSize)
        {
            try
            {
                ReadLine sizeExceeded = ReadLine.BufferSufficient;
                if (_stream == null)
                {
                    if (!InitializeNewStream())
                    {
                        Trace.TraceError("InternalReadLineFromCurrentPositionToEnd - initialize stream failed");
                        return (String.Empty, ReadLine.BufferSufficient);
                    }
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
                    sizeExceeded = ReadLine.ReadLineContentExceedsSize;
                }
                if (toRead <= 0)
                    return (String.Empty, ReadLine.BufferSufficient);

                CheckIfBufferNeedsReallocation(toRead);

                Span<byte> buffer = _localBuffer.AsSpan<byte>().Slice(0, (int)toRead);
                var read = _stream.Read(buffer);

                var lastIndex = buffer.LastIndexOf((byte)'\n');

                if (lastIndex < 0)
                {
                    _currentPosition = current;
                    return (String.Empty, ReadLine.BufferSufficient);
                }

                SetCurrentPositionAndResetBufferIfNeeded(ref buffer, ref lastIndex);

                string result = System.Text.Encoding.Default.GetString(buffer);
                return (result, sizeExceeded);
            }
            catch (Exception e)
            {
                Console.Error.Write(e.Message);
                return (String.Empty, ReadLine.BufferSufficient);
            }
        }

        private void SetCurrentPositionAndResetBufferIfNeeded(ref Span<byte> buffer, ref int lastIndex)
        {
            ++lastIndex; // Return also the \n character

            if (lastIndex < buffer.Length)
            {
                // We couldn't read a complete line at the end - so position to the last index
                _currentPosition += lastIndex;
                if (_currentPosition < 0)
                {
                    // That shouldn't happen ?!
                    _currentPosition = 0;
                }

                buffer = _localBuffer.AsSpan<byte>().Slice(0, lastIndex);
            }
            else
                _currentPosition = _stream.Position;
        }

        private void CheckIfBufferNeedsReallocation(long toRead)
        {
            if ((_localBuffer == null) || (_localBuffer.Length < toRead))
            {
                _localBuffer = new byte[toRead];
                _reallocCounter = 0;
            }
            else
            {
                // Following lines shall prevent a really big buffer of memory to be held forever if not needed
                if (_localBuffer.Length > (toRead * 2))
                    ++_reallocCounter;
                else _reallocCounter = 0;
                if (_reallocCounter > ReallocAfterXCountsLowerThan50Percent)
                {
                    _reallocCounter = 0;
                    _localBuffer = new byte[toRead];
                }
            }

        }
    }


}
