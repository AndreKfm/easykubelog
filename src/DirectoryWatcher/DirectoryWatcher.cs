using System;
using System.IO;

namespace DirectoryWatcher
{
    [Flags]
    public enum IFileSystemWatcherFilter
    {
        None = 0,
        LastAccess = 1, 
        LastWrite = 2, 
        FileName = 3,
        DirectoryName = 4
    }


    public enum IFileSystemWatcherChangeType
    {
        Created, Rename, Deleted, Changed
    }

    public interface IFileSystemWatcher : IDisposable
    {
        bool Open(string path);
        bool Open(string path, string fileFilter);
    }

    public class FileSystemWatcherWrapper : IFileSystemWatcher
    {
        FileSystemWatcher _watcher;
        public FileSystemWatcherWrapper()
        {
        }

        public void Dispose()
        {
            if (_watcher != null) _watcher.Changed -= WatcherChanged;
            _watcher?.Dispose();
            _watcher = null;
        }

        public bool Open(string path)
        {
            return Open(path, String.Empty);
        }

        public bool Open(string path, string fileFilter)
        {            
            try
            {
                _watcher?.Dispose();
                _watcher = null;

                // Let's better pass only one argument in case that implementation in FileSystemWatcher is different
                _watcher = fileFilter == String.Empty ? new FileSystemWatcher(path) : 
                                                        new FileSystemWatcher(path, fileFilter);

                _watcher.Changed += WatcherChanged;
                _watcher.Created += WatcherDeleted;
                _watcher.Deleted += WatcherDeleted;
                _watcher.Disposed += WatcherDisposed;
                _watcher.Renamed += WatcherDisposed;
                _watcher.Error   += WatcherError;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void WatcherDisposed(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void WatcherError(object sender, ErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void WatcherDeleted(object sender, FileSystemEventArgs e)
        {
            _callbackFileSystemChanged.Invoke(this, IFileSystemWatcherChangeType.Deleted, e);
        }

        private void WatcherChanged(object sender, FileSystemEventArgs e)
        {
            _callbackFileSystemChanged.Invoke(this, IFileSystemWatcherChangeType.Changed, e);
        }

        Action<object, IFileSystemWatcherChangeType, FileSystemEventArgs> _callbackFileSystemChanged;
    }

    public class DirectoryWatcher : IDisposable
    {


        public DirectoryWatcher(IFileSystemWatcher watcher = null)
        {
            _watcher = watcher != null ? 
                watcher : // This allows mocking of watcher
                new FileSystemWatcherWrapper();
        }


        public bool Open(string path)
        {
            return _watcher.Open(path);
        }

        public bool Open(string path, string fileFilter)
        {
            return _watcher.Open(path, fileFilter);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _watcher = null;
        }

        IFileSystemWatcher _watcher;
    }
}
