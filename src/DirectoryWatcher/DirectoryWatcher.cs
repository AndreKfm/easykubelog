using System;
using System.IO;

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
        public WatcherCallbackArgs(string name, IFileSystemWatcherChangeType changeType)
        {
            Name = name;
            ChangeType = changeType;
        }
        public string Name { get; private set; }

        public IFileSystemWatcherChangeType ChangeType { get; private set; }
    }


    //
    // Interface for a file watcher which is watching a single directory (no subdirectories for changes)
    public interface IFileSystemWatcher : IDisposable
    {
        bool Open(string directoryPath, FilterAndCallbackArgument callbackAndFilter = null);

    }

    public class PhysicalFileSystemWatcherWrapper : IFileSystemWatcher
    {
        FileSystemWatcher _watcher;
        public PhysicalFileSystemWatcherWrapper()
        {
        }

        public void Dispose()
        {
            DisableWatcher();
            SetCallback(null);
        }

        /// <summary>
        /// Helper function which is called by dispose but also open function 
        /// </summary>
        private void DisableWatcher()
        {
            WatcherRemoveEvents();
            _watcher?.Dispose();
            _watcher = null;
        }



        private void WatcherSetEvents()
        {
            if (_watcher == null) return;
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed += WatcherChanged;
            _watcher.Created += WatcherCreated;
            _watcher.Deleted += WatcherDeleted;
            _watcher.Disposed += WatcherDisposed;
            _watcher.Renamed += WatcherRenamed;
            _watcher.Error += WatcherError;
            _watcher.IncludeSubdirectories = false;
            _watcher.InternalBufferSize = 65536; // Reserve for a larger number of containers running

            if (this._callbackFileSystemChanged != null)
                _watcher.EnableRaisingEvents = true;

        }

        private void WatcherRemoveEvents()
        {
            if (_watcher == null) return;
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= WatcherChanged;
            _watcher.Created -= WatcherCreated; ;
            _watcher.Deleted -= WatcherDeleted;
            _watcher.Disposed -= WatcherDisposed;
            _watcher.Renamed -= WatcherRenamed; ;
            _watcher.Error -= WatcherError;

        }


        public void Close()
        {
            WatcherRemoveEvents();
            SetCallback(null);
            _watcher?.Dispose();
            _watcher = null;

        }

        public void SetCallback(Action<object, WatcherCallbackArgs> action)
        {
            _watcher.EnableRaisingEvents = false; 
            _callbackFileSystemChanged = action;
            WatcherSetEvents();
        }

        private void WatcherDisposed(object sender, EventArgs e)
        {
            // Pass information in FileSystemEventArgs to keep interface simple
            _callbackFileSystemChanged?.Invoke(this, new WatcherCallbackArgs(String.Empty, IFileSystemWatcherChangeType.Dispose));
        }

        private void WatcherError(object sender, ErrorEventArgs e)
        {
            // Pass information in FileSystemEventArgs to keep interface simple
            _callbackFileSystemChanged?.Invoke(this, new WatcherCallbackArgs(e.GetException().ToString(), IFileSystemWatcherChangeType.Error));
        }

        private void WatcherDeleted(object sender, FileSystemEventArgs e)
        {
            _callbackFileSystemChanged?.Invoke(this, new WatcherCallbackArgs(e.Name, IFileSystemWatcherChangeType.Deleted));
        }

        private void WatcherChanged(object sender, FileSystemEventArgs e)
        {
            _callbackFileSystemChanged?.Invoke(this, new WatcherCallbackArgs(e.Name, IFileSystemWatcherChangeType.Changed));
        }
        private void WatcherCreated(object sender, FileSystemEventArgs e)
        {
            _callbackFileSystemChanged?.Invoke(this, new WatcherCallbackArgs(e.Name, IFileSystemWatcherChangeType.Changed));
        }

        private void WatcherRenamed(object sender, RenamedEventArgs e)
        {
            _callbackFileSystemChanged?.Invoke(this, new WatcherCallbackArgs(e.Name, IFileSystemWatcherChangeType.Rename));
        }


        public bool Open(string directoryPath, FilterAndCallbackArgument callbackAndFilter)
        {
            try
            {

                DisableWatcher();
                string fileFilter = callbackAndFilter != null ? callbackAndFilter.fileFilter : String.Empty;

                // Let's better pass only one argument in case that implementation in FileSystemWatcher is different
                _watcher = fileFilter == String.Empty ? new FileSystemWatcher(directoryPath) :
                                                        new FileSystemWatcher(directoryPath, fileFilter);

                if (callbackAndFilter != null)
                    SetCallback(callbackAndFilter.action);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        Action<object, WatcherCallbackArgs> _callbackFileSystemChanged;
    }

    public class DirectoryWatcher : IDisposable
    {


        public DirectoryWatcher(IFileSystemWatcher watcher = null)
        {
            _watcher = watcher ?? new PhysicalFileSystemWatcherWrapper();
        }



        public bool Open(string path, FilterAndCallbackArgument filterAndCallback = null)
        {
            return _watcher.Open(path, filterAndCallback);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _watcher = null;
        }

        IFileSystemWatcher _watcher;
    }
}
