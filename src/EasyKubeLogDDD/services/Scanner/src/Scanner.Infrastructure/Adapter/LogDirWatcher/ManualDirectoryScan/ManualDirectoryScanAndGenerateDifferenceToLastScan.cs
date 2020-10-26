using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Scanner.Domain.Ports;
using Scanner.Domain.Shared;

namespace Scanner.Infrastructure.Adapter.LogDirWatcher.ManualDirectoryScan
{
    /// <summary>
    /// Implements a simulation of the physical file system watcher, by manually scanning the directory
    /// for file changes periodically. 
    /// 
    /// This will also work with hard and softlinks!!!
    /// </summary>
    public class ManualDirectoryScanAndGenerateDifferenceToLastScan : IDirectoryFileScanner
    {

        private readonly ManualDirectoryScanAndGenerateDifferenceToLastScanSettings _settings;
        private readonly ManualScanDirectoryDifferences _diffs;
        private readonly IManualScanDirectory _scanDirectory;
        private readonly List<FileEntry> _changeQueue;


        public ManualDirectoryScanAndGenerateDifferenceToLastScan(ManualDirectoryScanAndGenerateDifferenceToLastScanSettings watcherSettings,
            IManualScanDirectory scanDirectory)
        {
            _settings = watcherSettings;
            _scanDirectory = scanDirectory;
            _diffs = new ManualScanDirectoryDifferences();
            _changeQueue = new List<FileEntry>();
            _currentDirectoryScan = null;
        }


        public ReadOnlyCollection<FileEntry> GetChangedFiles()
        {
            return new ReadOnlyCollection<FileEntry>(new List<FileEntry>(_changeQueue));
        }
     

        private Dictionary<string, long>? _currentDirectoryScan;
        public string GetCurrentDirectory()
        {
            return _settings.ScanDirectory;
        }

        public void ScanDirectory()
        {
            try
            {
                _currentDirectoryScan ??= new Dictionary<string, long>();
                _changeQueue.Clear();
                Trace.TraceInformation($"Scanning now directory: {_settings.ScanDirectory}");
                var fileListNew = _scanDirectory.Scan(_settings.ScanDirectory);
                AggregateChanges(_currentDirectoryScan, fileListNew);
                _currentDirectoryScan = fileListNew;
            }
            catch (Exception e)
            {
                Trace.TraceError($"Failed to open scan directory: exception [{e.Message}]");
            }
        }


        private void AggregateChanges(Dictionary<string, long> oldList, Dictionary<string, long> newList)
        {
            try
            {
                var changed = _diffs.GetChangedFiles(oldList, newList);
                ReportChangeType(changed, FileSystemWatcherChangeType.Changed);
                var newFiles = _diffs.GetNewFiles(oldList, newList);
                ReportChangeType(newFiles, FileSystemWatcherChangeType.Created);
                var deletedFiles = _diffs.GetDeletedFiles(oldList, newList);
                ReportChangeType(deletedFiles, FileSystemWatcherChangeType.Deleted);
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

        private void ReportChangeType(IEnumerable<KeyValuePair<string, long>> current,
            FileSystemWatcherChangeType changeType)
        {
            foreach (var file in current)
            {
                Trace.TraceInformation($"Reporting changes in directory: {_settings.ScanDirectory}  file: {file} changetype: {changeType}");
                _changeQueue.Add(new FileEntry{ FileName=file.Key, ChangeType = changeType });
            }
        }



    }
}