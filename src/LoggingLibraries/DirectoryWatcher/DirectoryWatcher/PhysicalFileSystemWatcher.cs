using System;
using System.IO;

using System.Collections.Generic;
using System.Text;

namespace DirectoryWatching
{

    /// <summary>
    /// Uses .NET Core supported file system watcher, which under Linux uses the very performant
    /// inotify interface
    /// 
    /// !!! DOES NOT SUPPORT A DIRECTORY HOLDING SOFT OR HARDLINKS !!!
    /// </summary>     
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
            if (_watcher != null) _watcher.EnableRaisingEvents = false;
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
            _callbackFileSystemChanged?.Invoke(this, new WatcherCallbackArgs(e.Name, IFileSystemWatcherChangeType.Created));
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
                _watcher = new FileSystemWatcher(directoryPath);
                //new FileSystemWatcher(directoryPath, fileFilter);

                _watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Attributes | NotifyFilters.Size;
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
}
