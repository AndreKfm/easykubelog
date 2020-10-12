using LogEntries;
using System;
using System.Threading.Tasks;

namespace EasyKubeLogService.Services.CentralLogService
{
    public interface ICentralLogServiceQuery : IDisposable
    {
        public KubernetesLogEntry[] Query(QueryParams queryParams);
    }

    public interface ICentralLogService : IDisposable
    {
        ValueTask<bool> AddLogEntry(LogEntry newEntry);

        public void Start();

        public void Stop();
    }

    public interface ICentralLogServiceCache : ICentralLogServiceQuery
    {
        public void AddEntry(LogEntry entry);
    }

    public enum CacheQueryMode
    {
        // ReSharper disable once UnusedMember.Global
        CaseSensitive, CaseInsensitive
    }

    public readonly struct TimeRange
    {
        public TimeRange(DateTimeOffset from, DateTimeOffset to)
        {
            From = from;
            To = to;
        }

        public bool IsDefault()
        {
            return From == default && To == default;
        }

        public bool IsInBetweenOrDefault(DateTimeOffset time)
        {
            return (time == default || IsDefault() == true) || (
                   (From == default || From <= time) &&
                   (To == default   || To >= time));
        }


        public readonly DateTimeOffset From { get; }
        public readonly DateTimeOffset To { get; }
    }

    public readonly struct QueryParams
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

    public interface ICache<in TKey, in TValue> : IDisposable
    {
        void Add(TKey key, TValue value);

        void Flush();

        KubernetesLogEntry[] Query(QueryParams queryParams, CacheQueryMode mode);
    }
}