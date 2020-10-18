using System;
using System.Collections.Generic;
using System.Text;

namespace Scanner.Domain.Shared
{
    [Flags]
    public enum FileSystemWatcherChangeType
    {
        Created = 1,
        Rename = 2,
        Deleted = 4,
        Changed = 8,
        Dispose = 16,
        Error = 32,
        All = Created | Rename | Deleted | Changed | Dispose | Error
    }

    public struct FileEntry
    {
        public FileEntry(string fileName, FileSystemWatcherChangeType changeType)
        {
            FileName = fileName;
            ChangeType = changeType;
        }
        public string FileName { get; private set; }
        public FileSystemWatcherChangeType ChangeType { get; private set; }
    }
}
