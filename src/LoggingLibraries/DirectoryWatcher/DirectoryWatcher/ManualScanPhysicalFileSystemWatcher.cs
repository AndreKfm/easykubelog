using DirectoryWatcher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    using FileList = Dictionary<string, long>;
    using FileListEnum = IEnumerable<KeyValuePair<string, long>>;

    public class ManualScanPhysicalFileSystemWatcherSettings
    {

        private int _scanSpeedInSeconds;

        public int ScanSpeedInSeconds
        {
            // Don't allow values of 0 or lower, because this would consume 
            // too much CPU cycles for nothing 
            get { return _scanSpeedInSeconds; }
            set { _scanSpeedInSeconds = value; if (_scanSpeedInSeconds < 0) _scanSpeedInSeconds = 0; }
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
        public FileListEnum GetNewFiles(FileList oldScanned, FileList newScanned)
        {
            return newScanned.Where(s => oldScanned.ContainsKey(s.Key) == false);
        }

        public FileListEnum GetDeletedFiles(FileList oldScanned, FileList newScanned)
        {
            return oldScanned.Where(s => newScanned.ContainsKey(s.Key) == false);
        }

        public FileListEnum GetChangedFiles(FileList oldScanned, FileList newScanned)
        {
            return newScanned.Where(s => oldScanned.ContainsKey(s.Key) && (oldScanned[s.Key] != s.Value));
        }
    }


    public interface IManualScanDirectory
    {
        public FileList Scan(string directory);
    }

    public class ManualScanDirectory : IManualScanDirectory
    {
        public ManualScanDirectory()
        {
        }

        public FileList ScanFastWithFileInfo(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            FileList list = new FileList();
            foreach(var file in files)
            {
                var fileInfo = new FileInfo(file);
                try
                {
                    var length = fileInfo.Length;
                    // var lastWriteUtc = fileInfo.LastWriteTimeUtc; Not needed anymore - since not reliable with file links
                    list.Add(file, length);
                }
                catch (Exception) { }
            }
            return list;
        }

        public FileList Scan(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            FileList list = new FileList();
            foreach (var file in files)
            {
                using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                try
                {
                    var length = fileStream.Length; // This ensures to get the changed length from the destination file a link is pointing to
                    list.Add(file, length);
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

        ManualScanPhysicalFileSystemWatcherSettings _settings;
        ManualScanDirectoryDifferences _diffs = new ManualScanDirectoryDifferences();
        IManualScanDirectory _scanDirectory;
        Task _currentFileSystemWatcher = null;

        public ManualScanPhysicalFileSystemWatcher(ManualScanPhysicalFileSystemWatcherSettings watcherSettings = null,
                                                   IManualScanDirectory scanDirectory = null)
        {
            _settings = watcherSettings ?? new ManualScanPhysicalFileSystemWatcherSettings();
            _scanDirectory = scanDirectory ?? new ManualScanDirectory();
        }


        public void Dispose()
        {
            Stop();
        }

        CancellationTokenSource _tokenSource;
        private async Task PeriodicallyScanDirectory(CancellationToken token, FilterAndCallbackArgument callbackAndFilter)
        {
            int scanMs = _settings.ScanSpeedInSeconds * 1000;
            if (scanMs == 0)
                scanMs = 100;
            string scanDir = _settings.ScanDirectory;
            var current = _scanDirectory.Scan(scanDir);
            while (token.IsCancellationRequested == false)
            {
                try
                {
                    await Task.Delay(scanMs);
                    if (callbackAndFilter.ActionScanning != null)
                        callbackAndFilter.ActionScanning(this);
                    Trace.TraceInformation($"Scanning now directory: {_settings.ScanDirectory}");
                    var fileListNew = _scanDirectory.Scan(scanDir);
                    ReportChanges(current, fileListNew, token, callbackAndFilter);
                    current = fileListNew;
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Failed to scan directory: exception [{e.Message}]");
                }

            }
        }

        private void ReportChanges(FileList oldList, FileList newList, CancellationToken token, FilterAndCallbackArgument callbackAndFilter)
        {
            try
            {
                var Report = callbackAndFilter.ActionChanges;
                var changed = _diffs.GetChangedFiles(oldList, newList);
                ReportChangeType(changed, token, Report, IFileSystemWatcherChangeType.Changed);
                var newFiles = _diffs.GetNewFiles(oldList, newList);
                ReportChangeType(newFiles, token, Report, IFileSystemWatcherChangeType.Created);
                var deletedFiles = _diffs.GetDeletedFiles(oldList, newList);
                ReportChangeType(deletedFiles, token, Report, IFileSystemWatcherChangeType.Deleted);
            }
            catch(OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            { }
        }

        private void ReportChangeType(FileListEnum current, 
                                      CancellationToken token, 
                                      Action<object, WatcherCallbackArgs> Report, 
                                      IFileSystemWatcherChangeType changeType)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException();
            foreach (var file in current)
            {
                Trace.TraceInformation($"Reporting changes in directory: {_settings.ScanDirectory}  file: {file} changetype: {changeType}");
                Report(this, new WatcherCallbackArgs(file.Key, changeType));
            }
        }


        private void Stop()
        {
            if (_tokenSource != null && _tokenSource.IsCancellationRequested == false)
                Trace.TraceInformation($"Stop scanning directory: {_settings.ScanDirectory}");
            _tokenSource?.Cancel();
            _currentFileSystemWatcher?.Wait();
            _currentFileSystemWatcher = null;
            _tokenSource = null;
        }

        public bool Open(FilterAndCallbackArgument callbackAndFilter)
        {
            Stop();
            Trace.TraceInformation($"Open directory for scanning: {_settings.ScanDirectory} - scanning period: {_settings.ScanSpeedInSeconds} seconds");
            _tokenSource = new CancellationTokenSource();
            _currentFileSystemWatcher = Task.Factory.StartNew(
                async () => await PeriodicallyScanDirectory(_tokenSource.Token, callbackAndFilter), 
                TaskCreationOptions.LongRunning).Result;

            return true;
        }
    }

}
