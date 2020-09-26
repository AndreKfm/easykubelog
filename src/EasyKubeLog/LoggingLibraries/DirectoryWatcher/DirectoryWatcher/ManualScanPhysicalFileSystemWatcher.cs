using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable All

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

        public string ScanDirectory { get; set; }
    }

    public class ManualScanPhysicalFileSystemWatcherFileListSettings
    {
        public ManualScanPhysicalFileSystemWatcherFileListSettings()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Ensure under Windows that we have always the same casing to prevent double 
                // entries which Windows cannot differentiate, but this list would
                NormalizeFileName = s => Path.GetFileName(s).ToLower();
            }
            else
            {
                NormalizeFileName = Path.GetFileName;
            }
        }

        public Func<string, string> NormalizeFileName { get; set; }
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
        private readonly Dictionary<string, (DateTime lastWriteUtc, long fileLen)> _files = new Dictionary<string, (DateTime lastWriteUtc, long fileLen)>();

        private readonly Func<string, string> _normalizeFileName;

        public ManualScanPhysicalFileSystemWatcherFileList(ManualScanPhysicalFileSystemWatcherFileListSettings settings = null)
        {
            settings ??= new ManualScanPhysicalFileSystemWatcherFileListSettings();
            _normalizeFileName = settings.NormalizeFileName;
        }

        public bool AddFileTruncPath(string fileName, DateTime initial = default, long fileLen = long.MinValue)
        {
            try
            {
                fileName = _normalizeFileName(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
                if (_files.ContainsKey(fileName))
                    return false;
                _files.Add(fileName, (initial.ToUniversalTime(), fileLen));
                return true;
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        public bool RemoveFileIgnorePath(string fileName)
        {
            try
            {
                fileName = _normalizeFileName(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
                if (_files.ContainsKey(fileName))
                {
                    _files.Remove(fileName);
                    return true;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        public bool SetOrAddFileInfo(string fileName, (DateTime newOffset, long fileLength) fileInfo)
        {
            fileName = _normalizeFileName(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
            try
            {
                fileName = _normalizeFileName(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
                _files[fileName] = fileInfo;
                return true;
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        public Dictionary<string, (DateTime lastWriteUtc, long fileLength)> GetFileListCopy()
        {
            return new Dictionary<string, (DateTime lastWriteUtc, long fileLength)>(_files);
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
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                try
                {
                    var length = fileInfo.Length;
                    // var lastWriteUtc = fileInfo.LastWriteTimeUtc; Not needed anymore - since not reliable with file links
                    list.Add(file, length);
                }
                catch (Exception)
                {
                    // ignored
                }
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
                catch (Exception)
                {
                    // ignored
                }
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

        private readonly ManualScanPhysicalFileSystemWatcherSettings _settings;
        private readonly ManualScanDirectoryDifferences _diffs = new ManualScanDirectoryDifferences();
        private readonly IManualScanDirectory _scanDirectory;
        private Task _currentFileSystemWatcher = null;

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
            try
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
                        await Task.Delay(scanMs, token);
                        if (token.IsCancellationRequested)
                            break;
                        callbackAndFilter.ActionScanning?.Invoke(this);
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
            catch(Exception e)
            {
                Trace.TraceError($"Failed to open scan directory: exception [{e.Message}]");
            }
        }

        private void ReportChanges(FileList oldList, FileList newList, CancellationToken token, FilterAndCallbackArgument callbackAndFilter)
        {
            try
            {
                var report = callbackAndFilter.ActionChanges;
                var changed = _diffs.GetChangedFiles(oldList, newList);
                ReportChangeType(changed, token, report, FileSystemWatcherChangeType.Changed);
                var newFiles = _diffs.GetNewFiles(oldList, newList);
                ReportChangeType(newFiles, token, report, FileSystemWatcherChangeType.Created);
                var deletedFiles = _diffs.GetDeletedFiles(oldList, newList);
                ReportChangeType(deletedFiles, token, report, FileSystemWatcherChangeType.Deleted);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void ReportChangeType(FileListEnum current,
                                      CancellationToken token,
                                      Action<object, WatcherCallbackArgs> report,
                                      FileSystemWatcherChangeType changeType)
        {
            if (token.IsCancellationRequested)
                throw new OperationCanceledException();
            foreach (var file in current)
            {
                Trace.TraceInformation($"Reporting changes in directory: {_settings.ScanDirectory}  file: {file} changetype: {changeType}");
                report(this, new WatcherCallbackArgs(file.Key, changeType));
            }
        }


        private void Stop()
        {
            try
            {
                if (_tokenSource != null && _tokenSource.IsCancellationRequested == false)
                    Trace.TraceInformation($"Stop scanning directory: {_settings.ScanDirectory}");
                try { _tokenSource?.Cancel(); }
                catch (Exception)
                {
                    // ignored
                }

                _currentFileSystemWatcher?.Wait();
                _currentFileSystemWatcher = null;
                _tokenSource = null;
            }
            catch(Exception e)
            {
                Trace.TraceError($"Exception stopping file watcher {e.Message}");
            }
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
