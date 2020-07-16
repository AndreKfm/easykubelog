using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using C5;
using Microsoft.Extensions.Logging;

namespace EasyLogService.Services.CentralLogService
{
    public class CentralLogServiceCache : ICentralLogServiceCache
    {
        readonly int _maxLines;
        readonly ILogger<CentralLogServiceCache> _logger;
        public CentralLogServiceCache(int maxLines, ILogger<CentralLogServiceCache> logger)
        {
            _maxLines = maxLines;
            _logger = logger;
        }

        readonly Dictionary<string, int> _fileIndexList = new Dictionary<string, int>();
        int _currentFileIndex = 0;

        readonly TreeDictionary<(DateTimeOffset time, int fileIndex), KubernetesLogEntry> _logCache = new TreeDictionary<(DateTimeOffset time, int fileIndex), KubernetesLogEntry>();
        public void AddEntry(LogEntry entry)
        {
            var lines = entry.Lines.Split('\n');
            foreach (string line in lines)
            {
                if (line.Length > 0)
                {
                    var newEntry = KubernetesLogEntry.Parse(line);
                    if (!newEntry.IsDefault)
                    {
                        lock (_logCache)
                        {
                            if (!_fileIndexList.TryGetValue(entry.FileName, out int fileIndex))
                            {
                                fileIndex = ++_currentFileIndex;
                                _fileIndexList.Add(entry.FileName, fileIndex);
                            }
                            if (_logCache.Count > _maxLines)
                                _logCache.Remove(_logCache.First().Key);
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
            lock (_logCache)
            {
                var result = _logCache.AsParallel().
                    Where(x => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.Value.Log, simpleQuery, CompareOptions.IgnoreCase) >= 0).
                    Take(maxResults).
                    Select(x => x.Value).
                    OrderBy(x => x.Time);
                //var result = _logCache.Where(x => x.Value.log.Contains(simpleQuery)).Select(x => x.Value);
                return result.ToArray();
            }
        }

        public void Dispose()
        {
            _logCache.Clear();
        }
    }
}
