using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileToolsClasses
{
    interface IFileStreamFactory
    {
        IStreamReaderWrapper Create();
    }

    public readonly struct FileModes
    {
        public FileModes(FileMode mode, FileAccess access, FileShare share)
        {
            Mode = mode;
            Access = access;
            Share = share;
        }
        public FileMode Mode { get; }
        public FileAccess Access { get; }
        public FileShare Share { get; }

    }

    public class StreamReaderWrapperFactoryImpl : IFileStreamFactory
    {
        private readonly string _fileName;
        private readonly FileModes _modes;

        public StreamReaderWrapperFactoryImpl(string fileName, FileModes modes)
        {
            _fileName = fileName;
            _modes = modes;
        }

        public IStreamReaderWrapper Create()
        {
            return new StreamReaderWrapper(File.Open(_fileName, _modes.Mode, _modes.Access, _modes.Share));
        }
    }


    public interface IStreamReaderWrapper : IDisposable
    {
        public long Seek(long offset, SeekOrigin origin);
        public string ReadLine();
    }

    public class StreamReaderWrapper : IStreamReaderWrapper, IDisposable
    {
        private readonly StreamReader _reader;
        public StreamReaderWrapper(FileStream file)
        {
            _reader = new StreamReader(file);
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }

        public long Seek(long offset, SeekOrigin origin)
        {
            return _reader.BaseStream.Seek(offset, origin);
        }

        public string ReadLine()
        {
            return _reader.ReadLine();
        }
    }

}
