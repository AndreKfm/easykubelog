using C5;
using LogEntries;
using System;
using System.Globalization;
using System.Linq;

namespace EasyKubeLogService.Services.CentralLogService
{
    // ReSharper disable once UnusedMember.Global
    public class MemoryCacheTreeDictionary : ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry>
    {
        private readonly int _maxLines;

        public MemoryCacheTreeDictionary(int maxLines)
        {
            _maxLines = maxLines;
        }

        private readonly TreeDictionary<(DateTimeOffset, int fileIndex), KubernetesLogEntry> _logCache = new TreeDictionary<(DateTimeOffset, int fileIndex), KubernetesLogEntry>();

        public void Add((DateTimeOffset, int fileIndex) key, KubernetesLogEntry value)
        {
            lock (_logCache)
            {
                if (_logCache.Count > _maxLines)
                    _logCache.Remove(_logCache.First().Key);
                _logCache.Add(key, value);
            }
        }

        public void Flush()
        {
            // Not really needed - memory needs no flush from applications
        }

        public void Dispose()
        {
            _logCache.Clear();
        }

        private bool CheckInBetween(KubernetesLogEntry k, TimeRange timeRange)
        {
            return timeRange.IsInBetweenOrDefault(k.Time);
        }

        private KubernetesLogEntry[] QueryCaseSensitive(string simpleQuery, int maxResults, TimeRange timeRange)
        {
            lock (_logCache)
            {
                var result = _logCache.AsParallel().
                    Where(x => CheckInBetween(x.Value, timeRange)).
                    Where(x => x.Value.Line.Contains(simpleQuery)).
                    Take(maxResults).
                    Select(x => x.Value).
                    OrderByDescending(x => x.Time);
                //var result = _logCache.Where(x => x.Value.Contains(simpleQuery)).Select(x => x.Value);
                return result.ToArray();
            }
        }

        private KubernetesLogEntry[] QueryCaseInSensitive(string simpleQuery, int maxResults, TimeRange timeRange)
        {
            lock (_logCache)
            {
                var result = _logCache.AsParallel().
                    Where(x => CheckInBetween(x.Value, timeRange)).
                    Where(x => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.Value.Line, simpleQuery, CompareOptions.IgnoreCase) >= 0).
                    Take(maxResults).
                    Select(x => x.Value).
                    OrderByDescending(x => x.Time);
                //var result = _logCache.Where(x => x.Value.Contains(simpleQuery)).Select(x => x.Value);
                return result.ToArray();
            }
        }

        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults, CacheQueryMode mode, TimeRange timeRange)
        {
            if (mode == CacheQueryMode.CaseInsensitive) return QueryCaseInSensitive(simpleQuery, maxResults, timeRange);
            return QueryCaseSensitive(simpleQuery, maxResults, timeRange);
        }
    }
}