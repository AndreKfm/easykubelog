using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasyLogService.Services.CentralLogService
{
    public class LogEntry
    {
        // Unformatted log entry directly read from log files 
        public LogEntry(string fileName, string lines)
        {
            FileName = fileName;
            Lines = lines;
        }

        public readonly string FileName; // File or container name
        public readonly string Lines;    // Log lines to add - either a single line or multiple lines separated by {CR}LF
    }

    internal class KubernetesJsonDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTimeOffset.Parse(reader.GetString());
        }

        // This method is not needed but has to be implemented 
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    // Log entry in Kubernetes format - reads itself with Json deserializer from a log entry (commonly a single line) 
    public class KubernetesLogEntry
    {

        private static JsonSerializerOptions InitOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new KubernetesJsonDateTimeOffsetConverter());
            //options.PropertyNameCaseInsensitive = true;
            return options;
        }

        private static readonly JsonSerializerOptions Options = InitOptions();

        private static readonly KubernetesLogEntry Default = new KubernetesLogEntry { Time = default, Log = String.Empty, Stream = String.Empty };

        static public KubernetesLogEntry Parse(string line)
        {
            try
            {
                if (line.Length > 0)
                    return JsonSerializer.Deserialize<KubernetesLogEntry>(line, Options);
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Parse: {e.Message}"); }
            return Default;
        }

        public bool IsDefault => Stream == String.Empty && Log == String.Empty;

        [JsonPropertyName("log")]
        public string Log { get; set; } // Log lines to add

        [JsonPropertyName("stream")]
        public string Stream { get; set; } // Type of log

        [JsonPropertyName("time")]
        public DateTimeOffset Time { get; set; }  // Date time when log entry was written on client side - use string to preserve ticks


    }

}
