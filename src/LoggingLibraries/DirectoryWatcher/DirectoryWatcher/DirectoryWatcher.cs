using System;

namespace DirectoryWatching
{


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
