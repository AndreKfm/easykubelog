using C5;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EasyLogService.Services
{
    public class LogEntry
    {
        public LogEntry(string fileName, string lines)
        {
            FileName = fileName;
            Lines = lines; 
        }

        public readonly string FileName; // File or container name
        public readonly string Lines;    // Log lines to add 
    }


    public class KubernetesLogEntry
    {

        static readonly KubernetesLogEntry Default = new KubernetesLogEntry { time = default(DateTime), log = String.Empty, stream = String.Empty };

        static public KubernetesLogEntry Parse(string line)
        {
            try
            {
                var replaced = line.Replace("\r", ""); // Ensure to ignore carriage returns
                if (replaced.Trim().Length > 0)
                    return JsonSerializer.Deserialize<KubernetesLogEntry>(replaced);
            }
            catch (Exception) { }
            return Default;
        }

        public bool IsDefault => stream == String.Empty && log == String.Empty;

        public string log { get; set; } // Log lines to add
        public string stream { get; set; } // Type of log
        public DateTime time { get; set; }  // Date time when log entry was written on client side
    }

    public interface ICentralLogService : IDisposable
    {
        Task<bool> AddLogEntry(LogEntry newEntry);
    }





    public class CentralLogServiceCache
    {
        TreeDictionary<DateTime, KubernetesLogEntry> _logCache = new TreeDictionary<DateTime, KubernetesLogEntry>();
        public void AddEntry(LogEntry entry)
        {
            var lines = entry.Lines.Split('\n');
            foreach (string line in lines)
            {
                if (line.Trim().Length > 0)
                {
                    var newEntry = KubernetesLogEntry.Parse(line);
                    if (!newEntry.IsDefault)
                    {
                        // 
                    }
                }
            }
            //_logCache.Add()
        }


    }

    /// <summary>
    /// This class holds the logs passed to EasyLogService
    /// </summary>
    public class CentralLogService : ICentralLogService
    {
        /// <summary>
        /// Creates a central object used to aggregate all incomming log entries
        /// </summary>
        /// <param name="maxEntriesInChannelQueue">Specifies how man entries can be added asynchronously to the channgel</param>
        public CentralLogService(int maxEntriesInChannelQueue = 1024)
        {
            _logEntryChannel = Channel.CreateBounded<LogEntry>(maxEntriesInChannelQueue);
        }


        void Start()
        {
            Stop();
            _currentTask = Task.Factory.StartNew(WaitForNewEntriesAndWrite, TaskCreationOptions.LongRunning);
        }

        void Stop()
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

        Channel<LogEntry> _logEntryChannel;
    }
}
