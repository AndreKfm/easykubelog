using System;

namespace DirectoryWatcher
{

    public class FileDirectoryWatcherSettings
    {
        public FileDirectoryWatcherSettings() { }
        public FileDirectoryWatcherSettings(string scanDir) { ScanDirectory = scanDir; }
        public string ScanDirectory { get; set; }

        public bool UseManualScan { get; set; } = false; // By default use physical scanning
        public int MaxContentLenghtToForwardForEachScanInBytes { get; set; } = 65536;
        public int ScanIntervalInSeconds { get; set; } = 10; // Will be used only if UseManualScan is set - specifies how often the manual scanner
                                                             // scans the specified [ScanDirectory] directory
    }


    public class FileDirectoryWatcher : IDisposable
    {
        FileDirectoryWatcherSettings _settings;
        IFileSystemWatcher _watcher;

        public FileDirectoryWatcher(FileDirectoryWatcherSettings settings, IFileSystemWatcher watcher = null)
        {
            _settings = settings;
            _watcher = watcher ?? CreateWatcher();
        }

        private IFileSystemWatcher CreateWatcher()
        {
            if (_settings.UseManualScan == true)
                return new ManualScanPhysicalFileSystemWatcher(new ManualScanPhysicalFileSystemWatcherSettings { ScanSpeedInSeconds = _settings.ScanIntervalInSeconds, ScanDirectory = _settings.ScanDirectory });
            else
                return new PhysicalFileSystemWatcherWrapper(new PhysicalFileSystemWatcherWrapperSettings { ScanDirectory = _settings.ScanDirectory });
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

    }
}
