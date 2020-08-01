using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using C5;
using LogEntries;

namespace EasyLogService.Services.CentralLogService
{

    public class MemoryCacheTreeDictionary : ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry>
    {
        private readonly int _maxLines;
        public MemoryCacheTreeDictionary(int maxLines)
        {
            _maxLines = maxLines;
        }

        readonly TreeDictionary<(DateTimeOffset, int fileIndex), KubernetesLogEntry> _logCache = new TreeDictionary<(DateTimeOffset, int fileIndex), KubernetesLogEntry>();

        public void Add((DateTimeOffset, int fileIndex) key, KubernetesLogEntry value)
        {
            lock (_logCache)
            {
                if (_logCache.Count > _maxLines)
                    _logCache.Remove(_logCache.First().Key);
                _logCache.Add(key, value);
            }
        }

        public void Dispose()
        {
            _logCache.Clear();
        }

        private bool CheckInBetween(KubernetesLogEntry k, DateTimeOffset from, DateTimeOffset to)
        {
            return (from == default || from <= k.Time) && (to == default || to >= k.Time);
        }

        private KubernetesLogEntry[] QueryCaseSensitive(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {
            lock (_logCache)
            {
                var result = _logCache.AsParallel().
                    Where(x => CheckInBetween(x.Value, from, to)).
                    Where(x => x.Value.Log.Contains(simpleQuery)).
                    Take(maxResults).
                    Select(x => x.Value).
                    OrderBy(x => x.Time);
                //var result = _logCache.Where(x => x.Value.log.Contains(simpleQuery)).Select(x => x.Value);
                return result.ToArray();
            }
        }


        private KubernetesLogEntry[] QueryCaseInSensitive(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {
            lock (_logCache)
            {
                var result = _logCache.AsParallel().
                    Where(x => CheckInBetween(x.Value, from, to)).
                    Where(x => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.Value.Log, simpleQuery, CompareOptions.IgnoreCase) >= 0).
                    Take(maxResults).
                    Select(x => x.Value).
                    OrderBy(x => x.Time);
                //var result = _logCache.Where(x => x.Value.log.Contains(simpleQuery)).Select(x => x.Value);
                return result.ToArray();
            }
        }
        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults, CacheQueryMode mode, DateTimeOffset from, DateTimeOffset to)
        {
            if (mode == CacheQueryMode.CaseInsensitive) return QueryCaseInSensitive(simpleQuery, maxResults, from, to);
            return QueryCaseSensitive(simpleQuery, maxResults, from, to);
        }
    }
}
