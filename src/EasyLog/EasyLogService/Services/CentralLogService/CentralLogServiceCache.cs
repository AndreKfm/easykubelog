using System;
using System.Collections.Generic;
using LogEntries;
using Microsoft.Extensions.Logging;

namespace EasyLogService.Services.CentralLogService
{



    public class CentralLogServiceCache : ICentralLogServiceCache
    {
        readonly Dictionary<string, int> _fileIndexList = new Dictionary<string, int>();
        int _currentFileIndex = 0;
        readonly ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry> _logCache;
        readonly ILogger<CentralLogServiceCache> _logger;

        public CentralLogServiceCache(int maxLines, ILogger<CentralLogServiceCache> logger, ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry> cache = null)
        {
            //_logCache = cache ?? new MemoryCacheTreeDictionary(maxLines);
            //_logCache = cache ?? new FileCache(@"c:\test\central_test.log", maxLines);
            var endlessStream = new EndlessFileStreamClasses.EndlessFileStream(@"C:\test\endless");
            _logCache = cache ?? new EndlessFileStreamCache(endlessStream, maxLines);
            //_logCache = cache ?? new FileCache(@"c:\test\central.log", maxLines);
            _logger = logger;
        }


        //readonly TreeDictionary<(DateTimeOffset time, int fileIndex), KubernetesLogEntry> _logCache = new TreeDictionary<(DateTimeOffset time, int fileIndex), KubernetesLogEntry>();
        public void AddEntry(LogEntry entry)
        {
            var lines = entry.Lines.Split('\n');
            foreach (string line in lines)
            {
                if (line.Length > 0)
                {
                    var newEntry = KubernetesLogEntry.Parse(line);
                    if (!newEntry.IsDefault())
                    {
                        lock (_logCache)
                        {
                            if (!_fileIndexList.TryGetValue(entry.FileName, out int fileIndex))
                            {
                                fileIndex = ++_currentFileIndex;
                                _fileIndexList.Add(entry.FileName, fileIndex);
                            }
                            try
                            {
                                _logCache.Add((newEntry.Time, fileIndex), newEntry);
                            }
                            catch (Exception)
                            {
                                // Sometimes - for what reason ever there are multiple entries in log files 
                                // so convert this message into an internal exception error and pass the original log entry 
                                // in the error message itself

                                const int MaxStringLength = 200;
                                var logLine = newEntry.Log.Length > MaxStringLength ? newEntry.Log.Substring(0, MaxStringLength) + "..." : newEntry.Log;

                                var dummyEntry = new KubernetesLogEntry
                                {
                                    Log = $"EASYLOGERROR: Exception - entry with the same time added already - original text: {logLine}",
                                    Time = newEntry.Time,
                                    Stream = "EASYLOG"
                                };

                                _logger.LogError($"Could not write log entry: {dummyEntry.Time} : { logLine }");

                                try
                                {
                                    _logCache.Add((newEntry.Time, 0), dummyEntry);
                                }
                                catch(Exception)
                                {
                                    try
                                    {
                                        dummyEntry.Log = "EASYLOGERROR: SECOND exception: " + dummyEntry.Log;
                                        _logCache.Add((DateTimeOffset.Now, 0), dummyEntry);
                                    }
                                    catch (Exception)
                                    {
                                        _logger.LogError($"Could not write log entry multiple times: {dummyEntry.Time} : { dummyEntry.Log }");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }




        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults)
        {
            return _logCache.Query(simpleQuery, maxResults, CacheQueryMode.CaseInsensitive);
        }

        public void Dispose()
        {
            _logCache.Dispose();
        }
    }
}
