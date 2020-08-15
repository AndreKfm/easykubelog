using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

    public interface IFile
    {
        string ReadLineFromCurrentPositionToEnd(long maxStringSize = 16384); // Read all data as string from current position to the last occurence
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

        IFileStreamReader GetStreamReader();
        bool SeekLastLineFromCurrentAndPositionOnStartOfIt();

    }
}
