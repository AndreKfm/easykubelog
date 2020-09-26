using System;
using System.Collections.Immutable;

namespace EndlessFileStream
{
    using FileListType = ImmutableSortedDictionary<long, EndlessStreamFileListEntry>;

    //internal class KubernetesJsonDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    //{
    //    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //    {
    //        return DateTimeOffset.Parse(reader.GetString());
    //    }

    //    // This method is not needed but has to be implemented 
    //    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
    //public class KubernetesLogEntry
    //{

    //    private static JsonSerializerOptions InitOptions()
    //    {
    //        JsonSerializerOptions options = new JsonSerializerOptions();
    //        options.Converters.Add(new KubernetesJsonDateTimeOffsetConverter());
    //        //options.PropertyNameCaseInsensitive = true;
    //        return options;
    //    }

    //    private static readonly JsonSerializerOptions Options = InitOptions();

    //    private static readonly KubernetesLogEntry Default = new KubernetesLogEntry { Time = default, Log = String.Empty, Stream = String.Empty, Container = String.Empty };

    //    static public KubernetesLogEntry Parse(string line)
    //    {
    //        try
    //        {
    //            if (line.Length > 0)
    //                return JsonSerializer.Deserialize<KubernetesLogEntry>(line, Options);
    //        }
    //        catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Parse: {e.Message}"); }
    //        return Default;
    //    }

    //    public void Write(StreamWriter writer)
    //    {
    //        try
    //        {
    //            string line = JsonSerializer.Serialize<KubernetesLogEntry>(this);
    //            writer.WriteLine(line);
    //        }
    //        catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Write: {e.Message}"); }
    //    }

    //    public void Write(Action<string> writer)
    //    {
    //        try
    //        {
    //            string line = JsonSerializer.Serialize<KubernetesLogEntry>(this);
    //            writer(line);
    //        }
    //        catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Write.Action: {e.Message}"); }
    //    }


    //    public bool IsDefault() { return Stream == String.Empty && Log == String.Empty && Container == String.Empty; }

    //    [JsonPropertyName("cont")]
    //    public string Container { get; set; } // Container name

    //    [JsonPropertyName("log")]
    //    public string Log { get; set; } // Log lines to add

    //    [JsonPropertyName("stream")]
    //    public string Stream { get; set; } // Type of log

    //    [JsonPropertyName("time")]
    //    public DateTimeOffset Time { get; set; }  // Date time when log entry was written on client side - use string to preserve ticks


    //}

    public class EndlessStreamFileListEntry
    {
        public string FileName { get; set; }
        public long FileIndex { get; set; }
    }

    public interface IEndlessFileStreamFileList
    {
        string GetLatestEntryFileName();
        FileListType GetFileList();
        FileListType AddNewFileDeleteOldestIfNeeded();
    }

    public interface IEndlessFileStreamNames
    {
        public string IndexListFileName { get; }
        public string IndexHashFileName { get; }
        public string FileTemplate { get; }
        public string GenerateNewFileName { get; }

    }


    public class EndlessFileStreamNames : IEndlessFileStreamNames
    {
        readonly string _baseDirectory;
        public EndlessFileStreamNames(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }
        public string IndexListFileName => $"{_baseDirectory}/index.txt";
        public string IndexHashFileName => $"{_baseDirectory}/index_hash.txt";



        public string FileTemplate => $"{_baseDirectory}/endless-*.log"; // Used to delete directory
        public string GenerateNewFileName => $"{_baseDirectory}/endless-{Guid.NewGuid()}.log";
    }


    public interface IEndlessFileStreamFileListOperations
    {
        public void WriteListToFile(FileListType fileList);
        public FileListType ReadListFromFile();
        public void PurgeRedundantFiles(FileListType list);
    }



}
