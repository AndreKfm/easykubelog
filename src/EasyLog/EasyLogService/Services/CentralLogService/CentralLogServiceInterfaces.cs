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


    // This converter is needed since JSON deserializer cannot parse log datetime entries with ticks added to it


}
