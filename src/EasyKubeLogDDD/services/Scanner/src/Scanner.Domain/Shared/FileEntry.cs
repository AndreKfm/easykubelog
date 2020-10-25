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

    public record FileEntry
    {
    public string FileName;
    public FileSystemWatcherChangeType ChangeType;
    }
}
