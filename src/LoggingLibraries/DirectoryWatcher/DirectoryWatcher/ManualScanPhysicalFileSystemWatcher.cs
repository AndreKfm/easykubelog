using DirectoryWatching;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryWatcher
{


    public class ManualScanPhysicalFileSystemWatcherSettings
    {

        private int _scanSpeedInSeconds;

        public int ScanSpeedInSeconds
        {
            // Don't allow values of 0 or lower, because this would consume 
            // too much CPU cycles for nothing 
            get { return _scanSpeedInSeconds; }
            set { _scanSpeedInSeconds = value; if (_scanSpeedInSeconds <= 0) _scanSpeedInSeconds = 1; }
        }

    }


    public class ManualScanPhysicalFileSystemWatcherFileList
    {
        // Holds file names [without (root) path] and the latest known file position
        ImmutableDictionary<string, long> files = ImmutableDictionary<string, long>.Empty;

        public ManualScanPhysicalFileSystemWatcherFileList()
        {

        }
    }


    /// <summary>
    /// Implements a simulation of the physical file system watcher, by manually scanning the directory
    /// for file changes periodically. 
    /// 
    /// This will also work with hard and softlinks!!!
    /// </summary>
    public class ManualScanPhysicalFileSystemWatcher : IFileSystemWatcher
    {

        ManualScanPhysicalFileSystemWatcherSettings settings;

        public ManualScanPhysicalFileSystemWatcher(ManualScanPhysicalFileSystemWatcherSettings watcherSettings = null, int scanSpeedInSeconds = 11)
        {
            settings = watcherSettings ?? new ManualScanPhysicalFileSystemWatcherSettings();
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        Task _currentFileSystemWatcher;

        public bool Open(string directoryPathToScanFiles, FilterAndCallbackArgument callbackAndFilter = null)
        {
            return false;
        }
    }

}
