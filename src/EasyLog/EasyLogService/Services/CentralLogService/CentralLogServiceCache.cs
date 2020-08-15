using System;
using System.Collections.Generic;
using System.Diagnostics;
using EndlessFileStreamClasses;
using LogEntries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyLogService.Services.CentralLogService
{


    public class CentralLogServiceCacheSettings
    {
        public string CentralMasterLogDirectory { get; set; }
        public long MaxLogFileSizeInMByte { get; set; } = 1024;
    }


    public class CentralLogServiceCache : ICentralLogServiceCache
    {
        readonly Dictionary<string, int> _fileIndexList = new Dictionary<string, int>();
        int _currentFileIndex = 0;
        readonly ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry> _logCache;
        readonly ILogger<CentralLogServiceCache> _logger;
        IParser _defaultParser = null;

        public CentralLogServiceCache(IOptions<CentralLogServiceCacheSettings> settings, 
                                      IConfiguration config, 
                                      ILogger<CentralLogServiceCache> logger, 
                                      ICache<(DateTimeOffset, int fileIndex), 
                                      KubernetesLogEntry> cache = null)
        {
            //_logCache = cache ?? new MemoryCacheTreeDictionary(maxLines);
            //_logCache = cache ?? new FileCache(@"c:\test\central_test.log", maxLines);

            EndlessFileStreamSettings endlessSettings = 
                new EndlessFileStreamSettings 
                { 
                    BaseDirectory = settings.Value.CentralMasterLogDirectory, 
                    MaxLogFileSizeInMByte = settings.Value.MaxLogFileSizeInMByte 
                };

            var endlessStream = new EndlessFileStreamClasses.EndlessFileStream(endlessSettings);
            _logCache = cache ?? new EndlessFileStreamCache(endlessStream);
            //_logCache = cache ?? new FileCache(@"c:\test\central.log", maxLines);
            _logger = logger;
        }


        //readonly TreeDictionary<(DateTimeOffset time, int fileIndex), KubernetesLogEntry> _logCache = new TreeDictionary<(DateTimeOffset time, int fileIndex), KubernetesLogEntry>();
        public void AddEntry(LogEntry entry)
        {
            if (entry.FileName.StartsWith("kube-system"))
            {
                Trace.TraceInformation($"Filtering out kube-system: [{entry.FileName}]");
                return;
            }
            var lines = entry.Lines.Split('\n');
            foreach (string line in lines)
            {
                if (line.Length > 0)
                {
                    var newEntry = KubernetesLogEntry.Parse(ref _defaultParser, line);
                    if (newEntry != null && !newEntry.IsDefault())
                    {
                        newEntry.SetContainerName(entry.FileName);
                        lock (_logCache)
                        {
                            InternalAddNewLogEntry(entry, newEntry);
                        }
                    }
                }
            }

            Flush();
        }

        private void Flush()
        {
            lock (_logCache)
            {
                _logCache.Flush();
            }
        }

        private void InternalAddNewLogEntry(LogEntry entry, KubernetesLogEntry newEntry)
        {
            if (!_fileIndexList.TryGetValue(entry.FileName, out int fileIndex))
            {
                Trace.TraceInformation($"Add new entry to cache: {entry.FileName} : {entry.Lines}");
                fileIndex = ++_currentFileIndex;
                _fileIndexList.Add(entry.FileName, fileIndex);
            }
            try
            {
                _logCache.Add((newEntry.Time, fileIndex), newEntry);
            }
            catch (Exception e)
            {
                Trace.TraceError($"Exception: {e.Message} while adding {entry.FileName} : {entry.Lines}");
                HandleAddEntryErrors(newEntry);
            }
        }

        private void HandleAddEntryErrors(KubernetesLogEntry newEntry)
        {
            // Sometimes - for what reason ever there are multiple entries in log files 
            // so convert this message into an internal exception error and pass the original log entry 
            // in the error message itself

            const int MaxStringLength = 200;
            var line = newEntry.Line;
            var logLine = line.Length > MaxStringLength ? line.Substring(0, MaxStringLength) + "..." : line;

            var log = new DockerLog
            {
                Line = $"EASYLOGERROR: Exception - entry with the same time added already - original text: {logLine}",
                Time = newEntry.Time,
                Stream = "EASYLOG"
            };

            var dummyEntry = new KubernetesLogEntry { SetLog = log};

            _logger.LogError($"Could not write log entry: {dummyEntry.Time} : { logLine }");

            try
            {
                _logCache.Add((newEntry.Time, 0), dummyEntry);
            }
            catch (Exception)
            {
                try
                {
                    dummyEntry.ReplaceLine("EASYLOGERROR: SECOND exception: " + dummyEntry.Line);
                    _logCache.Add((DateTimeOffset.Now, 0), dummyEntry);
                }
                catch (Exception)
                {
                    _logger.LogError($"Could not write log entry multiple times: {dummyEntry.Time} : { dummyEntry.Line }");
                }
            }
        }

        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {
            return _logCache.Query(simpleQuery, maxResults, CacheQueryMode.CaseInsensitive, from, to);
        }

        public void Dispose()
        {
            _logCache.Dispose();
        }
    }
}
