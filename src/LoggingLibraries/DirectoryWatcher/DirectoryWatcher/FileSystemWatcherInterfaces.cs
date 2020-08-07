using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryWatching
{
    [Flags]
    public enum IFileSystemWatcherChangeType
    {
        Created = 1,
        Rename = 2,
        Deleted = 4,
        Changed = 8,
        Dispose = 16,
        Error = 32,
        All = Created | Rename | Deleted | Changed | Dispose | Error
    }

    public class FilterAndCallbackArgument
    {

        public FilterAndCallbackArgument(string fileFilter,
                                         Action<object, WatcherCallbackArgs> action = null)
        {
            this.fileFilter = fileFilter;
            this.action = action;
        }

        public string fileFilter = String.Empty; // Specifies the files to watch = String.Empty = all files
        public readonly Action<object, WatcherCallbackArgs> action; // Callback to call on specified changes
    }

    public class WatcherCallbackArgs
    {
        public WatcherCallbackArgs(string fileName, IFileSystemWatcherChangeType changeType)
        {
            FileName = fileName;
            ChangeType = changeType;
        }
        public string FileName { get; private set; }

        public IFileSystemWatcherChangeType ChangeType { get; private set; }
    }


    //
    // Interface for a file watcher which is watching a single directory (no subdirectories for changes)
    public interface IFileSystemWatcher : IDisposable
    {
        bool Open(string directoryPath, FilterAndCallbackArgument callbackAndFilter = null);

    }
}
