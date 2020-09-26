using LogEntries;
using System;
using System.Threading.Tasks;

namespace EasyKubeLogService.Services.CentralLogService
{
    public interface ICentralLogServiceQuery : IDisposable
    {
        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to);
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

    public interface ICache<in TKey, in TValue> : IDisposable
    {
        void Add(TKey key, TValue value);

        void Flush();

        KubernetesLogEntry[] Query(string simpleQuery, int maxResults, CacheQueryMode mode, DateTimeOffset from, DateTimeOffset to);
    }
}