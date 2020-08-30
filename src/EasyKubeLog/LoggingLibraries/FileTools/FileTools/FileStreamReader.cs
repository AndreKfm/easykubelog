using System.IO;

namespace FileToolsClasses
{
    public class FileStreamReader : IFileStreamReader
    {
        StreamReader _reader;
        public FileStreamReader(FileStream stream)
        {
            _reader = new StreamReader(stream);
        }

        enum WhichPosition
        {
            OneBeforeIfNotStartOfFile, OneBehindIfNotStartOfFile
        }

    }
}
