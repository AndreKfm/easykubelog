using System;
using System.IO;

namespace FileToolsClasses
{
    public enum FileStreamDirection
    {
        Forward, Backwards
    }

    public interface IFileStreamReader
    {
    }
    public interface IFileStreamWriter
    {
        void Write(Span<byte> buffer);
    }

    public interface IGetFile
    {
        IFile GetFile(string fileName);
    }

    public enum ReadLine
    {
        BufferSufficient,
        ReadLineContentExceedsSize // Will be returned if the internal buffer was too small to read all data
    }
    public interface IFile
    {
        (string line, ReadLine sizeExceeded)
            ReadLineFromCurrentPositionToEnd(long maxStringSize = 6000); // Read all data as string from current position to the last occurrence
                                                                         // of '\n'. If '\n' is not found the whole string will be returned if maxStringSize
                                                                         // has been reached - otherwise an empty string will be returned and more data
                                                                         // on the next call if '\n' is found
    }
    public interface IFileStream : IDisposable
    {
        long Seek(long offset, SeekOrigin origin);
        long Position { get; set; }
        long Length { get; }
        int Read(Span<byte> buffer);
        int Read(byte[] buffer); // Mainly for unit test since [Span] is not "mock friendly"

        bool SeekLastLineFromCurrentAndPositionOnStartOfIt();

    }
}
