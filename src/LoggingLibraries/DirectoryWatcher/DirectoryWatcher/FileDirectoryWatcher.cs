using System;

namespace DirectoryWatcher
{

    public class FileDirectoryWatcherSettings
    {
        public FileDirectoryWatcherSettings() { }
        public FileDirectoryWatcherSettings(string scanDir) { ScanDirectory = scanDir;  }
        public string ScanDirectory { get; set; }
    }


    public class FileDirectoryWatcher : IDisposable
    {
        FileDirectoryWatcherSettings _settings; 
        public FileDirectoryWatcher(FileDirectoryWatcherSettings settings, IFileSystemWatcher watcher = null)
        {
            _settings = settings;
            _watcher = watcher ?? new PhysicalFileSystemWatcherWrapper(new PhysicalFileSystemWatcherWrapperSettings { ScanDirectory = _settings.ScanDirectory });
        }

        public bool Open(FilterAndCallbackArgument filterAndCallback = null)
        {
            return _watcher.Open(filterAndCallback);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _watcher = null;
        }

        IFileSystemWatcher _watcher;
    }
}
