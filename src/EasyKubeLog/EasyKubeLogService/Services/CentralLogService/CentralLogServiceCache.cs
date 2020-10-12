using LogEntries;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EasyKubeLogService.Services.CentralLogService
{
    public class CentralLogServiceCacheSettings
    {
        public string CentralMasterLogDirectory { get; set; }
        public long MaxLogFileSizeInMByte { get; set; } = 1024;
        public bool FlushWrite { get; set; } = true;
    }

    public class CentralLogServiceCache : ICentralLogServiceCache
    {
        private readonly Dictionary<string, int> _fileIndexList = new Dictionary<string, int>();
        private readonly ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry> _logCache;
        private readonly ILogger<CentralLogServiceCache> _logger;
        private int _currentFileIndex;
        private IParser _defaultParser;
        private readonly CentralLogServiceCacheSettings _settings;

        public CentralLogServiceCache(IOptions<CentralLogServiceCacheSettings> settings,
                                      ILogger<CentralLogServiceCache> logger,
                                      ICache<(DateTimeOffset, int fileIndex),
                                      KubernetesLogEntry> cache = null)
        {
            _settings = settings.Value;


            _logCache = cache ?? throw new Exception("Log cache not specified in Central log service cache");
            _logger = logger;
        }

        public void AddEntry(LogEntry entry)
        {
            if (entry.FileName.StartsWith("kube-system"))
            {
                Trace.TraceInformation($"Filtering out kube-system: [{entry.FileName}]");
                return;
            }
            var lines = entry.Lines.Split('\n');

            foreach (string line in lines.Where(s => s.Length > 0))
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

            Flush();
        }

        private void Flush()
        {
            if (_settings.FlushWrite == false)
                return;

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
            // Just in case that equal entries would have been written to the log file
            // handle them separately and write a log error message instead

            const int maxStringLength = 200;
            var line = newEntry.Line;
            var logLine = line.Length > maxStringLength ? line.Substring(0, maxStringLength) + "..." : line;

            var log = new DockerLog
            {
                // ReSharper disable StringLiteralTypo
                Line = $"EASYLOGERROR: Exception - entry with the same time added already - original text: {logLine}",
                Time = newEntry.Time,
                Stream = "EASYLOG"
            };

            var dummyEntry = new KubernetesLogEntry { SetLog = log };

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

        public KubernetesLogEntry[] Query(QueryParams queryParams)
        {
            lock (_logCache)
            {
                return _logCache.Query(queryParams, CacheQueryMode.CaseInsensitive);
            }
        }

        public void Dispose()
        {
            _logCache.Dispose();
        }
    }
}