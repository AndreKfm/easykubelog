using System;

namespace DirectoryWatcher
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
                                         Action<object, WatcherCallbackArgs> actionChanges = null,
                                         Action<object> actionScanning = null)
        {
            this.FileFilter = fileFilter;
            this.ActionChanges = actionChanges;
            this.ActionScanning = actionScanning;
        }

        public readonly string FileFilter = String.Empty;                   // Specifies the files to watch = String.Empty = all files
        public readonly Action<object, WatcherCallbackArgs> ActionChanges;  // Callback to call on specified changes
        public readonly Action<object> ActionScanning; // If set will be called each time a scan is executed
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
        bool Open(FilterAndCallbackArgument callbackAndFilter = null);

    }
}
