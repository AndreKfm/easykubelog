using System;

namespace DirectoryWatcher
{


    public class FileDirectoryWatcher : IDisposable
    {

        public FileDirectoryWatcher(IFileSystemWatcher watcher = null)
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
