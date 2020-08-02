using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogEntries
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

    // kube-apiserver-myserver-2_kube-system_kube-apiserver-1827c8c0196e15c01ed339eac252aa483212dfd1b25ce44d2fca974a954c196b.log
    // grafana-c6bfd5949-6g6h2_monitoring_grafana-8fdbfdebc4370290aaed8dc47782571bef9c8ac294e012a14af5546fb7df4f62.log
    public class KubernetesContainerNameTools
    {
        public static (string deployment, string containerName, string nm, string contId) DeserializeContainerName(string containerName)
        {
            var array = containerName.Split('_');
            if (array.Length < 3)
            {
                return ("#", "#", "#", "#");
            }
            int idPosition = array[2].LastIndexOf('-');
            var containerShortName = array[2].Substring(0, idPosition);
            var containerId = array[2].Substring(idPosition + 1);
            return (array[0], containerShortName, array[1], containerId);
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

        public void Write(Action<string> writer)
        {
            try
            {
                string line = JsonSerializer.Serialize<KubernetesLogEntry>(this);
                writer(line);
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Write.Action: {e.Message}"); }
        }
        public void Write(StreamWriter writer)
        {
            try
            {
                string line = JsonSerializer.Serialize<KubernetesLogEntry>(this);
                writer.WriteLine(line);
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Write: {e.Message}"); }
        }

        private static readonly JsonSerializerOptions Options = InitOptions();

        private static readonly KubernetesLogEntry Default = new KubernetesLogEntry { Time = default, Log = String.Empty, Stream = String.Empty, Container = String.Empty };

        static public KubernetesLogEntry Parse(string line, string optionalContainerName = null)
        {
            try
            {
                if (line.Length > 0)
                {
                    var k = JsonSerializer.Deserialize<KubernetesLogEntry>(line, Options);
                    if (String.IsNullOrEmpty(k.Container)) 
                        k.SetContainerName(optionalContainerName); // Only use external container name if not yet assigned
                    return k;
                }
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Parse: {e.Message}"); }
            return Default;
        }

        private static string StripContainerName(string containerName)
        {
            if ((containerName != null))
            {
                int index = containerName.LastIndexOf('.');
                if (index > 0)
                {
                    // Remove the filename extension if exists
                    containerName = containerName.Substring(0, index);
                }
            }
            return containerName;

        }
        

        public void SetContainerName(string container)
        {
            _containerName = StripContainerName(container);
        }

        public bool IsDefault() { return Stream == String.Empty && Log == String.Empty && Container == String.Empty; }

        private string _containerName = default;

        [JsonPropertyName("cont")]
        public string Container { get { return _containerName; } set { SetContainerName(value); } } // Container name

        [JsonPropertyName("log")]
        public string Log { get; set; } // Log lines to add

        [JsonPropertyName("stream")]
        public string Stream { get; set; } // Type of log

        [JsonPropertyName("time")]
        public DateTimeOffset Time { get; set; }  // Date time when log entry was written on client side - use string to preserve ticks


    }

}
