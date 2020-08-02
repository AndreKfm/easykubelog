using LogEntries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EasyLogService.Services.CentralLogService
{

    //
    // CentralLogService: It's safe to call AddLogEntry in parallel
    //    
    //                          +--------------+
    //                          |              |       +------------
    //    +-------------+       |    Central   |       |  Central  |
    //    | AddLogEntry | +---> |    Log       | +---> |  Log      |
    //    +-------------+       |    Service   |       |  Cache    |
    //                          |              |       +------------
    //                          |              |
    //                          |              |
    //                          +---+------+---+
    //                              |      ^
    //                              v      |
    //                          +---+------+---+
    //                          |              |
    //                          |   Channel    |
    //                          |              |
    //                          +--------------+
    //    

    /// <summary>
    /// This class holds the logs passed to EasyLogService
    /// </summary>
    public class CentralLogService : ICentralLogService, ICentralLogServiceQuery
    {

        Channel<LogEntry> _logEntryChannel;
        readonly ICentralLogServiceCache _cache;

        /// <summary>
        /// Creates a central object used to aggregate all incomming log entries
        /// </summary>
        /// <param name="maxEntriesInChannelQueue">Specifies how man entries can be added asynchronously to the channgel</param>
        public CentralLogService(ILogger<CentralLogServiceCache> logger, IConfiguration config, ICentralLogServiceCache cache = null, int maxEntriesInChannelQueue = 1024)
        {
            _logEntryChannel = Channel.CreateBounded<LogEntry>(maxEntriesInChannelQueue);
            _cache = cache ?? new CentralLogServiceCache(maxEntriesInChannelQueue, config, logger);
        }


        public void Start()
        {
            Stop();
            _source = new CancellationTokenSource();
            _currentTask = Task.Factory.StartNew(WaitForNewEntriesAndWrite, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            _source.Cancel();
            _currentTask.Wait();

        }

        Task _currentTask = Task.CompletedTask;
        CancellationTokenSource _source = new CancellationTokenSource();

        async Task WaitForNewEntriesAndWrite()
        {
            var token = _source.Token;
            while (token.IsCancellationRequested == false)
            {
                var available = await _logEntryChannel.Reader.WaitToReadAsync(token);
                if (!available) // If false the channel is closed
                    break;

                var newEntry = await _logEntryChannel.Reader.ReadAsync();
                _cache.AddEntry(newEntry);

            }
        }

        public async Task<bool> AddLogEntry(LogEntry newEntry)
        {
            return await Task.FromResult(_logEntryChannel.Writer.TryWrite(newEntry));
        }

        public void Dispose()
        {
            _logEntryChannel.Writer.Complete();
            _logEntryChannel = null;
        }


        KubernetesLogEntry[] ICentralLogServiceQuery.Query(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {
            return _cache.Query(simpleQuery, maxResults, from, to);
        }

    }
}
