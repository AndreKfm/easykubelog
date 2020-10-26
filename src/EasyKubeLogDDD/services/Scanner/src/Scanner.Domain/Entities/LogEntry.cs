using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel;


namespace Scanner.Domain.Entities
{

    internal class LogParserJsonDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonRead = reader.GetString();
            if (String.IsNullOrEmpty(jsonRead))
                return default;
            return DateTimeOffset.Parse(jsonRead);
        }

        // This method is not needed but has to be implemented 
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

    }
    internal record DockerLog
    {
    public static string NormalizeContainerName(string containerName)
        {
            int index = containerName.LastIndexOf('.');
            if (index > 0)
            {
                // Remove the filename extension if exists
                containerName = containerName.Substring(0, index);
            }
            return containerName;
        }
    }
    internal class LogParserContainerd : IParser
    {
        public LogEntry ParseLine(string line)
        {
            // 2020-08-09T16:19:56.1457454Z stdout F loga#27";
            var indexDateTimeEnd = line.IndexOf(' '); if (indexDateTimeEnd <= 0) return default;
            var indexStream = line.IndexOf(' ', indexDateTimeEnd + 1); if (indexStream <= 0) return default;
            var indexLog = line.IndexOf(' ', indexStream + 1); if (indexLog <= 0) return default;

            var dateTime = DateTime.Parse(line.Substring(0, indexDateTimeEnd - 1));
            var stream = line.Substring(indexDateTimeEnd + 1, indexStream - indexDateTimeEnd - 1);
            var log = line.Substring(indexLog + 1);

            return new LogEntry
            {
                CreateTime = dateTime,
                Stream = stream,
                Content = log,
                //Container = DockerLog.NormalizeContainerName(optionalContainerName ?? String.Empty)
            };
        }
    }
    internal class LogParserDocker : IParser
    {
        private static JsonSerializerOptions InitOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new LogParserJsonDateTimeOffsetConverter());
            //options.PropertyNameCaseInsensitive = true;
            return options;
        }

        private static readonly JsonSerializerOptions Options = InitOptions();

        public LogEntry ParseLine(string line)
        {
            try
            {
                if (line.Length > 0)
                {
                    return JsonSerializer.Deserialize<LogEntry>(line, Options);
                }
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Parse: {e.Message} - Line: {line}"); }
            return default;
        }
    }

    internal interface IParser
    {
        LogEntry ParseLine(string line);
    }

    internal class LogParserAutoDetect
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
            return new LogParserContainerd();
        }
    }

    public class LogEntry : Entity
    {
        public LogEntry(DateTimeOffset createTime, string content, string moduleName, string stream)
        {
            CreateTime = createTime;
            Content = content;
            ModuleName = moduleName;
            Stream = stream;
        }

        public LogEntry()
        {
            Content = String.Empty;
            CreateTime = default;
            ModuleName = String.Empty;
            Stream = String.Empty;
        }

        [JsonPropertyName("log")]
        public string Content { get; init; }

        [JsonPropertyName("time")]
        public DateTimeOffset CreateTime { get; init; }
        
        [JsonPropertyName("cont")]
        public string ModuleName { get; private set; }
        
        [JsonPropertyName("stream")]
        public string Stream { get; init; }

        public void SetModuleName(string moduleName)
        {
            ModuleName = moduleName;
        }



        private bool IsEmpty => ModuleName == default && Content == default && ModuleName == default;

        private static IParser _logParser; // Assuming that the system will use only one variant of log files !!

        public static LogEntry Parse(string line)
        {
            try
            {
                if (line.Length > 0)
                {
                    if (_logParser == null) 
                        _logParser = LogParserAutoDetect.GetAutoParser(line);
                    return  _logParser.ParseLine(line);
                }
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Parse: {e.Message} - Line: {line}"); }
            return new LogEntry();
        }

        // ReSharper disable once UnusedMember.Local
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
    }
}
