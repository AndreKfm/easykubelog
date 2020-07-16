using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyLogService.Services.CentralLogService
{
    public interface ICentralLogServiceQuery : IDisposable
    {
        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults);
    }


    public interface ICentralLogService : IDisposable
    {
        Task<bool> AddLogEntry(LogEntry newEntry);
        public void Start();
        public void Stop();
    }



    public interface ICentralLogServiceCache : ICentralLogServiceQuery
    {
        public void AddEntry(LogEntry entry);
    }


    public enum CacheQueryMode
    {
        CaseSensitive, CaseInsensitive
    }

    public interface ICache<Key, Value> : IDisposable
    {
        void Add(Key key, Value value);
        KubernetesLogEntry[] Query(string simpleQuery, int maxResults, CacheQueryMode mode);
    }



}
