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


        private KubernetesLogEntry[] LocalQuery(QueryParams queryParams, Func<KubernetesLogEntry, bool> compare)
        {
            lock (_logCache)
            {
                var result = _logCache.AsParallel().Where(x => 
                     CheckInBetween(x.Value, queryParams.Time))
                    .Where(x => compare(x.Value))
                    .Take(queryParams.MaxResults).Select(x => x.Value)
                    .OrderByDescending(x => x.Time);
                return result.ToArray();
            }
        }

        private KubernetesLogEntry[] QueryCaseSensitive(QueryParams queryParams)
        {
            bool Compare(KubernetesLogEntry k) => k.Line.Contains(queryParams.SimpleQuery);
            return LocalQuery(queryParams, Compare);
        }

        readonly struct QueryParams
        {
            public QueryParams(string simpleQuery, int maxResults, TimeRange timeRange)
            {
                SimpleQuery = simpleQuery;
                MaxResults = maxResults;
                Time = timeRange;
            }

            public readonly string SimpleQuery;
            public readonly int MaxResults;
            public readonly TimeRange Time;
        }

        private KubernetesLogEntry[] QueryCaseInSensitive(string simpleQuery, int maxResults, TimeRange timeRange)
        {
            var queryParams = new QueryParams(simpleQuery, maxResults, timeRange);
            bool Compare(KubernetesLogEntry k) => CultureInfo.CurrentCulture.CompareInfo.IndexOf(k.Line, simpleQuery, 
                CompareOptions.IgnoreCase) >= 0;
            return LocalQuery(queryParams, Compare);
        }

        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults, CacheQueryMode mode, TimeRange timeRange)
        {
            if (mode == CacheQueryMode.CaseInsensitive) return QueryCaseInSensitive(simpleQuery, maxResults, timeRange);
            return QueryCaseSensitive(new QueryParams(simpleQuery, maxResults, timeRange));
        }
    }
}