using DirectoryWatcher;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

    public class ManualScanPhysicalFileSystemWatcherFileListSettings
    {
        public ManualScanPhysicalFileSystemWatcherFileListSettings()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Ensure under Windows that we have always the same casing to prevent double 
                // entries which Windows cannot differentiate, but this list would
                _normalizeFileName = (string s) => Path.GetFileName(s).ToLower();
            }
            else
            {
                _normalizeFileName = (string s) => Path.GetFileName(s);
            }
        }
        private Func<string, string> _normalizeFileName;

        public Func<string, string> NormalizeFileName
        {
            get { return _normalizeFileName; }
            set { _normalizeFileName = value; }
        }

    }


    /// <summary>
    /// Holds a list of files without paths and their current read / write offset
    /// (interpretation of offset must be done by other classes - only one offset will be held)
    /// Path will be simple truncated - there is not verification of the path.
    /// If a file from another directory with the same file name will be tried to be added, 
    /// only one instance will be held 
    /// </summary>
    public class ManualScanPhysicalFileSystemWatcherFileList
    {
        // Holds file names [without (root) path] and the latest known file position
        Dictionary<string, long> files = new Dictionary<string, long>();

        Func<string, string> NormalizeFileName;

        public ManualScanPhysicalFileSystemWatcherFileList(ManualScanPhysicalFileSystemWatcherFileListSettings settings = null)
        {
            settings = settings ?? new ManualScanPhysicalFileSystemWatcherFileListSettings();
            NormalizeFileName = settings.NormalizeFileName;
        }

        public bool AddFileTruncPath(string fileName)
        {
            try
            {
                fileName = NormalizeFileName(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
                if (files.ContainsKey(fileName))
                    return false;
                files.Add(fileName, (long)0);
                return true;
            }
            catch (Exception) { }
            return false;
        }

        public bool RemoveFileIgnorePath(string fileName)
        {
            try
            {
                fileName = NormalizeFileName(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
                if (files.ContainsKey(fileName))
                {
                    files.Remove(fileName);
                    return true;
                }
            }
            catch(Exception) {}

            return false;
        }

        public bool SetOrAddFileOffset(string fileName, long newOffset)
        {
            if (newOffset < 0)
                return false;
            fileName = NormalizeFileName(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
            try
            {
                fileName = NormalizeFileName(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
                files[fileName] = newOffset;
                return true;
            }
            catch (Exception) { }

            return false;
        }

        public Dictionary<string, long> GetFileListCopy()
        {
            return new Dictionary<string, long>(files);
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
