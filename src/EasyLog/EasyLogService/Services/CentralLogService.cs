using C5;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    internal class KubernetesJsonDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class KubernetesLogEntry
    {

        private static JsonSerializerOptions InitOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new KubernetesJsonDateTimeConverter());
            return options;
        }

        private static JsonSerializerOptions Options = InitOptions();

        private static readonly KubernetesLogEntry Default = new KubernetesLogEntry { time = default(DateTime), log = String.Empty, stream = String.Empty };

        static public KubernetesLogEntry Parse(string line)
        {
            try
            {
                if (line.Length > 0)
                    return JsonSerializer.Deserialize<KubernetesLogEntry>(line, Options);
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Parse: {e.Message}" ); }
            return Default;
        }

        public bool IsDefault => stream == String.Empty && log == String.Empty;

        public string log { get; set; } // Log lines to add
        public string stream { get; set; } // Type of log
        public DateTime time { get; set; }  // Date time when log entry was written on client side
    }


    public interface ICentralLogServiceQuery : IDisposable
    {
        public KubernetesLogEntry[] Query(string simpleQuery);
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




    public class CentralLogServiceCache : ICentralLogServiceCache
    {
        readonly int _maxLines;
        public CentralLogServiceCache(int maxLines)
        {
            _maxLines = maxLines;
        }

        TreeDictionary<DateTime, KubernetesLogEntry> _logCache = new TreeDictionary<DateTime, KubernetesLogEntry>();
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
                            if (_logCache.Count > _maxLines)
                                _logCache.Remove(_logCache.First().Key);
                            _logCache.Add(newEntry.time, newEntry);
                        }
                    }
                }
            }
        }


        public KubernetesLogEntry[] Query(string simpleQuery)
        {
            lock (_logCache)
            {
                var result = _logCache.AsParallel().Where(x => x.Value.log.Contains(simpleQuery)).Select(x => x.Value);
                return result.ToArray();
            }
        }

        public void Dispose()
        {
            _logCache.Clear();
        }
    }

    /// <summary>
    /// This class holds the logs passed to EasyLogService
    /// </summary>
    public class CentralLogService : ICentralLogService, ICentralLogServiceQuery
    {

        ICentralLogServiceCache _cache;
        /// <summary>
        /// Creates a central object used to aggregate all incomming log entries
        /// </summary>
        /// <param name="maxEntriesInChannelQueue">Specifies how man entries can be added asynchronously to the channgel</param>
        public CentralLogService(ICentralLogServiceCache cache = null, int maxEntriesInChannelQueue = 1024)
        {
            _logEntryChannel = Channel.CreateBounded<LogEntry>(maxEntriesInChannelQueue);
            _cache = cache ?? new CentralLogServiceCache(maxEntriesInChannelQueue);
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


        KubernetesLogEntry[] ICentralLogServiceQuery.Query(string simpleQuery)
        {
            return _cache.Query(simpleQuery);
        }

        Channel<LogEntry> _logEntryChannel;
    }
}
