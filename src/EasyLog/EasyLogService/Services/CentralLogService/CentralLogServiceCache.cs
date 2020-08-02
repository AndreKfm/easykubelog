using System;
using System.Collections.Generic;
using LogEntries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EasyLogService.Services.CentralLogService
{



    public class CentralLogServiceCache : ICentralLogServiceCache
    {
        readonly Dictionary<string, int> _fileIndexList = new Dictionary<string, int>();
        int _currentFileIndex = 0;
        readonly ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry> _logCache;
        readonly ILogger<CentralLogServiceCache> _logger;

        public CentralLogServiceCache(int maxLines, IConfiguration config, ILogger<CentralLogServiceCache> logger, ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry> cache = null)
        {
            //_logCache = cache ?? new MemoryCacheTreeDictionary(maxLines);
            //_logCache = cache ?? new FileCache(@"c:\test\central_test.log", maxLines);
            var endlessStream = new EndlessFileStreamClasses.EndlessFileStream(config["EasyLogCentralLogDir"]);
            _logCache = cache ?? new EndlessFileStreamCache(endlessStream);
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
                        newEntry.SetContainerName(entry.FileName);
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
