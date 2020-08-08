using DirectoryWatcher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DirectoryWatcher
{
    //using FileListEntry = (string fileName, DateTime lastWriteUtc, long fileLength);
    using FileList = Dictionary<string, (DateTime lastWriteUtc, long fileLength)>;

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

        private string _baseDirectoryToScan;

        public string ScanDirectory
        {
            get { return _baseDirectoryToScan; }
            set { _baseDirectoryToScan = value; }
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
        Dictionary<string, (DateTime lastWriteUtc, long fileLen)> files = new Dictionary<string, (DateTime lastWriteUtc, long fileLen)>();

        Func<string, string> NormalizeFileName;

        public ManualScanPhysicalFileSystemWatcherFileList(ManualScanPhysicalFileSystemWatcherFileListSettings settings = null)
        {
            settings = settings ?? new ManualScanPhysicalFileSystemWatcherFileListSettings();
            NormalizeFileName = settings.NormalizeFileName;
        }

        public bool AddFileTruncPath(string fileName, DateTime initial = default, long fileLen = long.MinValue) 
        {
            try
            {
                fileName = NormalizeFileName(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
                if (files.ContainsKey(fileName))
                    return false;
                files.Add(fileName, (initial.ToUniversalTime(), fileLen));
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

        public bool SetOrAddFileInfo(string fileName, (DateTime newOffset, long fileLength) fileInfo)
        {
            fileName = NormalizeFileName(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
            try
            {
                fileName = NormalizeFileName(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
                files[fileName] = fileInfo;
                return true;
            }
            catch (Exception) { }

            return false;
        }

        public Dictionary<string, (DateTime lastWriteUtc, long fileLength)> GetFileListCopy()
        {
            return new Dictionary<string, (DateTime lastWriteUtc, long fileLength)>(files);
        }
    }


    public class ManualScanDirectoryDifferences
    {
        public IEnumerable<KeyValuePair<string, (DateTime lastWriteUtc, long fileLength)>> GetNewFiles(FileList oldScanned, FileList newScanned)
        {
            return newScanned.Where(s => oldScanned.ContainsKey(s.Key) == false);
        }

        public IEnumerable<KeyValuePair<string, (DateTime lastWriteUtc, long fileLength)>> GetDeletedFiles(FileList oldScanned, FileList newScanned)
        {
            return oldScanned.Where(s => newScanned.ContainsKey(s.Key) == false);
        }

        public IEnumerable<KeyValuePair<string, (DateTime lastWriteUtc, long fileLength)>> GetChangedFiles(FileList oldScanned, FileList newScanned)
        {
            return newScanned.Where(s => oldScanned.ContainsKey(s.Key) && (oldScanned[s.Key] != s.Value));
        }
    }


    public interface IManualScanDirectory
    {
        public List<(string fileName, DateTime lastWriteUtc, long fileLength)> Scan(string directory);
    }

    internal class ManualScanDirectory : IManualScanDirectory
    {
        public ManualScanDirectory()
        {
        }

        public List<(string fileName, DateTime lastWriteUtc, long fileLength)> Scan(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            List<(string fileName, DateTime lastWriteUtc, long fileLength)> list = new List<(string fileName, DateTime lastWriteUtc, long fileLength)>();
            foreach(var file in files)
            {
                var fileInfo = new FileInfo(file);
                try
                {
                    var length = fileInfo.Length;
                    var lastWriteUtc = fileInfo.LastWriteTimeUtc;
                    list.Add((file, lastWriteUtc, length));
                }
                catch (Exception) { }
            }
            return list;
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
        IManualScanDirectory _scanDirectory;
        Task _currentFileSystemWatcher = null;

        public ManualScanPhysicalFileSystemWatcher(IManualScanDirectory scanDirectory = null, 
                                                   ManualScanPhysicalFileSystemWatcherSettings watcherSettings = null)
        {
            settings = watcherSettings ?? new ManualScanPhysicalFileSystemWatcherSettings();
            _scanDirectory = scanDirectory ?? new ManualScanDirectory();
        }


        public void Dispose()
        {
            Stop();
        }

        CancellationTokenSource _tokenSource;
        private async Task PeriodicallyScanDirectory(CancellationToken token)
        {
            int scanMs = settings.ScanSpeedInSeconds * 1000;
            string scanDir = settings.ScanDirectory;
            var current = _scanDirectory.Scan(scanDir);
            while (token.IsCancellationRequested == false)
            {
                await Task.Delay(scanMs);
                var fileList = _scanDirectory.Scan(scanDir);
            }
        }

        private void Stop()
        {
            _tokenSource?.Cancel();
            _currentFileSystemWatcher?.Wait();
            _currentFileSystemWatcher = null;
            _tokenSource = null;
        }

        public bool Open(string directoryPathToScanFiles, FilterAndCallbackArgument callbackAndFilter = null)
        {
            Stop();
            _tokenSource = new CancellationTokenSource();
            _currentFileSystemWatcher = Task.Factory.StartNew(async () => await PeriodicallyScanDirectory(_tokenSource.Token), TaskCreationOptions.LongRunning);

            return false;
        }
    }

}
