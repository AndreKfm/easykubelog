using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LogEntries
{

    public class LogSamples
    {
        public const string ContainerDSample = @"2020-08-09T16:19:52.5938217Z stdout F root@loga1:/# echo loga#24
2020-08-09T16:19:52.5938818Z stdout F loga#24
2020-08-09T16:19:54.009581Z stdout F root@loga1:/# echo loga#25
2020-08-09T16:19:54.0096391Z stdout F loga#25
2020-08-09T16:19:55.3300352Z stdout F root@loga1:/# echo loga#26
2020-08-09T16:19:55.3300942Z stdout F loga#26
2020-08-09T16:19:56.1456899Z stdout F root@loga1:/# echo loga#27
2020-08-09T16:19:56.1457454Z stdout F loga#27";
    }


    
    public interface IParser
    {
        DockerLog ParseLine(string line, string optionalContainerName = default);
    }

    public struct DockerLog
    {
        [JsonPropertyName("cont")]
        public string Container { get; set; } // Container name

        [JsonPropertyName("log")]
        public string Line { get; set; } // Log lines to add

        [JsonPropertyName("stream")]
        public string Stream { get; set; } // Type of log

        [JsonPropertyName("time")]
        public DateTimeOffset Time { get; set; }  // Date time when log entry was written on client side - use string to preserve ticks

        static public string NormalizeContainerName(string containerName)
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
    }


    public class LogParserContainerd : IParser
    {
        public LogParserContainerd()
        {
        }

        public DockerLog ParseLine(string line, string optionalContainerName = default)
        {
            // 2020-08-09T16:19:56.1457454Z stdout F loga#27";
            var indexDateTimeEnd = line.IndexOf(' '); if (indexDateTimeEnd <= 0) return default;
            var indexStream = line.IndexOf(' ', indexDateTimeEnd + 1); if (indexStream <= 0) return default;
            var indexLog = line.IndexOf(' ', indexStream + 1); if (indexLog <= 0) return default;

            var dateTime = DateTime.Parse(line.Substring(0, indexDateTimeEnd - 1));
            var stream = line.Substring(indexDateTimeEnd + 1, indexStream - indexDateTimeEnd - 1);
            var log = line.Substring(indexLog + 1);

            return new DockerLog 
            { 
                Time = dateTime, 
                Stream = stream, 
                Line = log, 
                Container = DockerLog.NormalizeContainerName(optionalContainerName == default ? String.Empty : optionalContainerName) 
            };
        }
    }

    internal class LogParserJsonDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
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

    public class LogParserDocker : IParser
    {
        public LogParserDocker()
        {
        }

        private static JsonSerializerOptions InitOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new LogParserJsonDateTimeOffsetConverter());
            //options.PropertyNameCaseInsensitive = true;
            return options;
        }

        private static readonly JsonSerializerOptions Options = InitOptions();

        public DockerLog ParseLine(string line, string optionalContainerName)
        {
            try
            {
                if (line.Length > 0)
                {
                    var d = JsonSerializer.Deserialize<DockerLog>(line, Options);
                    if (String.IsNullOrEmpty(d.Container))
                        d.Container = DockerLog.NormalizeContainerName(optionalContainerName); // Only use external container name if not yet assigned
                    return d;
                }
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Parse: {e.Message} - Line: {line}"); }
            return default;
        }
    }


    public class LogParserAutoDetect
    {
        static bool Check(IParser parser, string line, out IParser parserOut)
        {
            if (parser != null && parser.ParseLine(line).Equals(default(DockerLog)) == false)
            {
                parserOut = parser; 
                return true;
            }
            parserOut = null;
            return false;            
        }

        public static IParser GetAutoParser(string line)
        {
            if (Check(new LogParserDocker(), line, out var retDocker)) return retDocker;
            if (Check(new LogParserContainerd(), line, out var retContainerd)) return retContainerd;
            return null;
        }
    }

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

        public static (string containerName, string nm, string podId) DeserializeContainerNameSimple(string fileEntry)
        {
            var array = fileEntry.Split('_');
            if (array.Length < 3)
            {
                return ("#", "#", "#");
            }
            var podId = array[2];
            var containerName = array[1];
            var nm = array[0];
            int indexOfPathEnd = podId.IndexOfAny(pathChars);
            if (indexOfPathEnd > 0)
            {
                podId = array[2].Substring(0, indexOfPathEnd);
            }
            return (containerName, nm, podId);
        }

        static private readonly char[] pathChars = new char[] { '\\', '/' };

    }

    // Log entry in Kubernetes format - reads itself with Json deserializer from a log entry (commonly a single line) 
    public class KubernetesLogEntry
    {
        private static JsonSerializerOptions InitOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new LogParserJsonDateTimeOffsetConverter());
            //options.PropertyNameCaseInsensitive = true;
            return options;
        }

        public void Write(Func<string, Task> writer)
        {
            try
            {
                string line = JsonSerializer.Serialize<DockerLog>(log);
                writer(line).Wait();
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Write.Action: {e.Message}"); }
        }
        public void Write(StreamWriter writer)
        {
            try
            {
                string line = JsonSerializer.Serialize<DockerLog>(log);
                writer.WriteLine(line);
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Write: {e.Message}"); }
        }

        private static readonly JsonSerializerOptions Options = InitOptions();

        private static readonly KubernetesLogEntry Default = new KubernetesLogEntry { log = default(DockerLog) };

        /// <summary>
        /// Parsing from POD logs
        /// </summary>
        static public KubernetesLogEntry Parse(ref IParser logParser, string line, string optionalContainerName = null)
        {
            try
            {
                if (line.Length > 0)
                {
                    KubernetesLogEntry k = new KubernetesLogEntry();
                    if (logParser == null)
                       logParser = LogParserAutoDetect.GetAutoParser(line);
                    if (logParser == null)
                        return default;
                    k.log = logParser.ParseLine(line, optionalContainerName);
                    return k;
                }
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Parse: {e.Message} - Line: {line}"); }
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
            log.Container = DockerLog.NormalizeContainerName(container);
        }

        public bool IsDefault() { return log.Line == default &&  log.Time == default; } // It's enough to check for these 2 entries - then by definition == default

        DockerLog log;

        public void ReplaceLine(string line) { log.Line = line; }

        public DockerLog SetLog { set { log = default; } }

        public string Container { get { return log.Container; } } // Container name

        public string Line { get { return log.Line; } } // Log lines to add

        public string Stream { get { return log.Stream; } } // Type of log

        public DateTimeOffset Time { get { return log.Time; } }  // Date time when log entry was written on client side - use string to preserve ticks


    }

}
