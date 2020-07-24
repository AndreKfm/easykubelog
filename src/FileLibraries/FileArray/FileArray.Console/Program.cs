using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FileArrayConsole
{

    using FileListType = ImmutableSortedDictionary<long, EndlessStreamFileListEntry>;

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

        private static readonly KubernetesLogEntry Default = new KubernetesLogEntry { Time = default, Log = String.Empty, Stream = String.Empty, Container = String.Empty };

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

        public void Write(StreamWriter writer)
        {
            try
            {
                string line = JsonSerializer.Serialize<KubernetesLogEntry>(this);
                writer.WriteLine(line);
            }
            catch (Exception e) { Console.Error.WriteLine($"Exception in KubernetesLogEntry.Write: {e.Message}"); }
        }

        public bool IsDefault() { return  Stream == String.Empty && Log == String.Empty && Container == String.Empty; }

        [JsonPropertyName("cont")]
        public string Container { get; set; } // Container name

        [JsonPropertyName("log")]
        public string Log { get; set; } // Log lines to add

        [JsonPropertyName("stream")]
        public string Stream { get; set; } // Type of log

        [JsonPropertyName("time")]
        public DateTimeOffset Time { get; set; }  // Date time when log entry was written on client side - use string to preserve ticks


    }

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
        public string FileTemplate { get; }
        public string GenerateNewFileName { get; }

    }


    public class EndlessFileStreamNames : IEndlessFileStreamNames
    {
        string _baseDirectory;
        public EndlessFileStreamNames(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }
        public string IndexListFileName => $"{_baseDirectory}/index.txt";

        public string FileTemplate => $"{_baseDirectory}/endless-*.log"; // Used to delete directory
        public string GenerateNewFileName => $"{_baseDirectory}/endless-{Guid.NewGuid()}.log";
    }


    public interface IEndlessFileStreamFileListOperations
    {
        public void WriteListToFile(FileListType fileList);
        public FileListType ReadListFromFile();
        public void PurgeRedundantFiles(FileListType list);
    }



    class EndlessFileStreamFileOperations : IEndlessFileStreamFileListOperations
    {
        IEndlessFileStreamNames _fileNames;

        public EndlessFileStreamFileOperations(IEndlessFileStreamNames fileNames)
        {
            _fileNames = fileNames;
        }


        public void WriteListToFile(FileListType fileList)
        {
            List<string> stringList = new List<string>();

            stringList.Add(string.Empty); // Dummy entry to be replaced
            var list = fileList;
            foreach (var entry in list.Skip(1))
            {
                entry.Value.FileName = Path.GetFullPath(entry.Value.FileName);
                string line = JsonSerializer.Serialize<EndlessStreamFileListEntry>(entry.Value);
                stringList.Add(line);
            }

            stringList[0] = CalcHashCodeString(stringList);

            File.WriteAllLines(_fileNames.IndexListFileName, stringList);
            PurgeRedundantFiles(fileList);
        }

        static string CalcHashCodeString(List<string> stringList)
        {
            if (stringList.Count <= 1)
                return String.Empty;
            string hashCodeList = String.Empty;
            for (int i = 1; i < stringList.Count; ++i)
            {
                hashCodeList += stringList[i].GetHashCode().ToString();
            }            
            return hashCodeList;
        }


        public FileListType ReadListFromFile()
        {

            if (!File.Exists(_fileNames.IndexListFileName))
                return null;

            var lines = File.ReadAllLines(_fileNames.IndexListFileName);
            var calculatedString = CalcHashCodeString(lines.ToList());

            if (lines.Length <= 1 || calculatedString != lines[0])
            {
                // Hash does not match - delete index and all files
                File.Delete(_fileNames.IndexListFileName);
                return null;
            }

            FileListType.Builder builder = System.Collections.Immutable.ImmutableSortedDictionary.CreateBuilder<long, EndlessStreamFileListEntry>(); 

            foreach (var line in lines)
            {
                var entry = JsonSerializer.Deserialize<EndlessStreamFileListEntry>(line);
                builder.Add(entry.FileIndex, entry);
            }
            var fileList = builder.ToImmutable();

            PurgeRedundantFiles(fileList);
            return fileList;
        }
        

        public void PurgeRedundantFiles(FileListType list) 
        {
            var fileTemplate = _fileNames.FileTemplate;
            var path = Path.GetDirectoryName(fileTemplate);
            var fileName = Path.GetFileName(fileTemplate);
            var files = Directory.GetFiles(path, fileName);

            HashSet<string> fileNameList = new HashSet<string>();
            foreach (var e in list) fileNameList.Add(Path.GetFullPath(e.Value.FileName));

            // Delete all old files matching the fileTemplate which are not found in the current index list
            foreach (var file in files)
            {
                if (fileNameList.Contains(Path.GetFullPath(file)) == false)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception) { }
                }
            }
        }

    }

    public class FileHelper
    {
        static public Func<string, FileStream> DefaultOpenFileRead = (string file) => File.OpenRead(file);
        static public Func<string, FileStream> DefaultOpenFileWrite = (string file) => File.OpenWrite(file);
        static public Func<string, FileStream> DefaultOpenFileCreate = (string file) => File.Create(file);

        static public IEnumerable<string> ReadFromFileStream(string fileName,
                                                             int maxLines,
                                                             Func<string, FileStream> OpenFile)
        {
            using (FileStream fileStream = OpenFile(fileName))
            using (StreamReader reader = new StreamReader(fileStream))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                for (; ; )
                {

                    string line = null;

                    try
                    {
                        line = reader.ReadLine();
                    }
                    catch (Exception)
                    {
                    }


                    if (line != null)
                        yield return line;
                    else break;
                }
            }
        }

        static public IEnumerable<string> ReadFromFileStream(string[] listToRead, int maxLines, Func<string, FileStream> OpenFile)
        {

            foreach (var file in listToRead)
            {
                foreach (var line in ReadFromFileStream(file, maxLines, OpenFile))
                {
                    yield return line; 
                }
            }
        }

    }

    /// <summary>
    /// Holds a list of files (written also to specified directory) 
    /// which represent the current log separated into these multiple files
    /// </summary>
    class EndlessFileStreamFileList : IEndlessFileStreamFileList
    {
        private int _maxEntries;
        private string _baseDirectory;
        private IEndlessFileStreamFileListOperations _fileOperations;
        private IEndlessFileStreamNames _fileNames;

        public EndlessFileStreamFileList(int maxEntries,
                                         string baseDirectory,
                                         IEndlessFileStreamFileListOperations fileOperations = null,
                                         IEndlessFileStreamNames fileNames = null)
        {
            _maxEntries = maxEntries;
            _fileNames = fileNames ?? new EndlessFileStreamNames(baseDirectory);
            _baseDirectory = baseDirectory;
            _fileOperations = fileOperations ?? new EndlessFileStreamFileOperations(_fileNames);
            _fileList = _fileOperations.ReadListFromFile() ?? AddNewFileDeleteOldestIfNeeded();
            PurgeRedundantFiles();
        }



        void PurgeRedundantFiles()
        {
            _fileOperations.PurgeRedundantFiles(_fileList);
        }

        public FileListType AddNewFileDeleteOldestIfNeeded()
        {
            var list = _fileList;
            long newIndex = 0;
            if (list.Count > 0)
            {
                var last = list.Values.Last();
                newIndex = last.FileIndex + 1;
            }

            string newFileName = _fileNames.GenerateNewFileName;

            list = list.Add(newIndex, new EndlessStreamFileListEntry { FileName = newFileName, FileIndex = newIndex });

            string fileToDelete = String.Empty;
            try
            {

                if (list.Count > _maxEntries)
                {
                    var toDelete = list.Values.First();
                    fileToDelete = toDelete.FileName;
                    try
                    {
                        File.Delete(fileToDelete);
                    }
                    catch(Exception)
                    {
                        // might happen if the file is currently read and cannot be deleted
                    }
                    list = list.Remove(toDelete.FileIndex);
                }
            }
            catch(Exception e)
            {
                Console.Error.WriteLine($"Exception in EndlessFileStreamFileList.AddNewFileDeleteOldestIfNeeded deleting oldest file: {fileToDelete} - exception: {e.Message}");
            }

            _fileList = list;
            _fileOperations.WriteListToFile(_fileList);
            PurgeRedundantFiles();
            return GetFileList();
        }

        public FileListType GetFileList()
        {
            return _fileList;
        }

        public string GetLatestEntryFileName()
        {
            var last = _fileList.Values.Last();
            return last.FileName; 
        }

        FileListType _fileList = FileListType.Empty;
    }

    public interface IEndlessFileStreamIO : IDisposable
    {
        public Task WriteToFileStream(string line);
        public IEnumerable<string> ReadFromFileStream(int maxLines);

    }

    public class EndlessFileStreamIO : IEndlessFileStreamIO
    {
        readonly string _baseDirectory;
        IEndlessFileStreamFileList _fileList;
        readonly IEndlessFileStreamNames _fileNames;
        long _maxLogFileSizeInBytesEachFile;
        FileStream _fileStream;
        private StreamWriter _writer;


        private IEndlessFileStreamFileListOperations _fileOperations;
        public EndlessFileStreamIO(string baseDirectory,
                                   long maxLogFileSizeInMBytes = 1024,
                                   IEndlessFileStreamFileList fileList = null,
                                   IEndlessFileStreamFileListOperations fileOperations = null,
                                   IEndlessFileStreamNames fileNames = null,
                                   int splitIntoCountFiles = 4)
        {
            _baseDirectory = baseDirectory;
            _fileNames = fileNames ?? new EndlessFileStreamNames(baseDirectory);
            _fileOperations = fileOperations ?? new EndlessFileStreamFileOperations(_fileNames);
            _fileList = fileList ?? new EndlessFileStreamFileList(splitIntoCountFiles, _baseDirectory, _fileOperations, _fileNames);
            if (splitIntoCountFiles <= 0)
                throw new ArgumentException($"Invalid number of files to split logfile into: {splitIntoCountFiles}");
            _maxLogFileSizeInBytesEachFile = maxLogFileSizeInMBytes * 1024 * 1024 / splitIntoCountFiles;
        }

        static void DisposeAndSetToNull<T>(ref T stream) where T : IDisposable
        {
            try
            {
                stream?.Dispose();
            }
            catch(Exception)
            {
            }
            stream = default(T);
        }

        FileStream Open(string fileName)
        {
            return File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        void InitCurrentFile()
        {
            var nameOfLatestFile = _fileList.GetLatestEntryFileName();
            Close();
            _fileStream = Open(nameOfLatestFile);
            _writer = new StreamWriter(_fileStream);
            
        }

        void Close()
        {
            DisposeAndSetToNull(ref _writer);
            _fileStream?.Dispose();
            _fileStream = null;
        }


        public async Task WriteToFileStream(string line)
        {
            long stringSize = System.Text.UTF8Encoding.Unicode.GetByteCount(line);

            await Writer().BaseStream.FlushAsync();
            long currentSize = _writer.BaseStream.Length;

            if ((currentSize + stringSize) > _maxLogFileSizeInBytesEachFile)
            {
                Close(); // Don't use file anymore after a new one has been created - otherwise files would stay open and couldn't be deleted 
                _fileList.AddNewFileDeleteOldestIfNeeded();                
            }

            Writer().WriteLine(line /** Default Encoding.UTF8 */);
        }

        public IEnumerable<string> ReadFromFileStream(int maxLines)

        {
            var listToRead = _fileList.GetFileList();
            var fileList = listToRead.Select(x => x.Value.FileName).ToArray();
            return FileHelper.ReadFromFileStream(fileList, maxLines, this.Open);
        }      





        StreamWriter Writer()
        {
            if (_writer == null) 
                InitCurrentFile();
            return _writer;
        }

        public void Dispose()
        {
            Close();
        }
    }

    public interface IEndlessFileStreamReader
    {
        public IEnumerable<string> ReadEntries(int maxLines = 100);
    }

    public class EndlessFileStreamReader : IEndlessFileStreamReader
    {
        IEndlessFileStreamIO _fileIO;

        public EndlessFileStreamReader(IEndlessFileStreamIO fileIO)
        {
            _fileIO = fileIO;
        }


        public IEnumerable<string> ReadEntries(int maxLines = 100)
        {
            return _fileIO.ReadFromFileStream(maxLines).Take(100);
        }
    }

    public interface IEndlessFileStreamWriter
    {
        public void WriteToFileStream(string line);
    }


    public class EndlessFileStreamWriter : IEndlessFileStreamWriter
    {
        IEndlessFileStreamIO _fileIO; 

        public EndlessFileStreamWriter(IEndlessFileStreamIO fileIO)
        {
            _fileIO = fileIO;
        }

        public void WriteToFileStream(string line)
        {
            var currentSize = _fileIO.WriteToFileStream(line);
        }
    }


    public class EndlessFileStream
    {
        IEndlessFileStreamWriter _writer;
        IEndlessFileStreamReader _reader;
        IEndlessFileStreamIO _io; 

        public EndlessFileStream(string baseDirectory, long maxLogFileSizeInMByte = 1024, IEndlessFileStreamWriter writer = null, IEndlessFileStreamReader reader = null)
        {
            _io = new EndlessFileStreamIO(baseDirectory, maxLogFileSizeInMByte);
            _writer = writer ?? new EndlessFileStreamWriter(_io);
            _reader = reader ?? new EndlessFileStreamReader(_io);
        }

        public IEndlessFileStreamWriter Writer { get { return _writer; } }
        public IEndlessFileStreamReader Reader { get { return _reader; } }
    }


    /// <summary>
    /// Helper class to build a central log file - can be used either for testing or on startup to generate a log file 
    /// from already existing logs
    /// </summary>
    public class EndlessFileStreamBuilder
    {
        public EndlessFileStreamBuilder()
        {
            
        }
        private class CompareAndAllowDoubleEntries : IComparer<long>
        {
            public int Compare(long left, long right)
            {
                return (right > left) ? -1 : 1; // no zeroes 
            }
        }

        private IComparer<long> _defaultComparer = new CompareAndAllowDoubleEntries();


        public IEnumerable<KubernetesLogEntry> EnumerateSortedByDateTime(IEnumerable<IEnumerable<string>> fileListEnumerationWithFileEntries)
        {
            List<IEnumerator<string>> iteratorList = new List<IEnumerator<string>>();

            foreach (var fileEnum in fileListEnumerationWithFileEntries)
            {
                var iterator = fileEnum.GetEnumerator();
                iterator.MoveNext();
                iteratorList.Add(iterator);
            }

            

            for (; ; )
            {
                SortedDictionary<long, (IEnumerator<string> iterator, KubernetesLogEntry entry)> sortList = 
                    new SortedDictionary<long, (IEnumerator<string> iterator, KubernetesLogEntry entry)>(_defaultComparer);
                foreach (var iterator in iteratorList)
                {
                    if (String.IsNullOrEmpty(iterator.Current) == false)
                    {
                        var k = KubernetesLogEntry.Parse(iterator.Current);
                        if (!k.IsDefault()) // Just to play it safe - remove empty lines
                        {
                            long ticks = k.Time.Ticks;
                            sortList.TryAdd(ticks, (iterator, k));
                        }
                    }
                }

                if (sortList.Count == 0)
                    break;


                var value = sortList.First().Value;
                if (value.iterator.MoveNext() == false)
                {
                    iteratorList.Remove(value.iterator);
                }

                yield return value.entry;
            }
        }


        public void GenerateOutputFile(string baseDirectory, string outputFile)
        {
            List<IEnumerable<string>> streams = OpenFiles(baseDirectory);

            var listEnumerator = streams.AsEnumerable();

            using (var fileStream = FileHelper.DefaultOpenFileCreate(outputFile))
            using (var writer = new StreamWriter(fileStream))
            {

                foreach (var s in EnumerateSortedByDateTime(listEnumerator))
                {
                    //if (s.IsDefault == false)
                        s.Write(writer);
                }
            }


        }


        List<IEnumerable<string>> OpenFiles(string baseDirectory)
        {
            var files = Directory.GetFiles(baseDirectory);
            List<IEnumerable<string>> streams = new List<IEnumerable<string>>();
            foreach (var file in files)
            {
                streams.Add(FileHelper.ReadFromFileStream(file, -1, (string file) => { return File.OpenRead(file); }));
            }

            return streams;
        }


    }


    class Program
    {

        static void ReadWhileWrite()
        {
            var stream = new EndlessFileStream(@"C:\test\FileArray", 1);
            Task w = Task.Run(() => TestWritingAndPerformance(stream));

            for (; ; )
            {
                try
                {
                    var entries = stream.Reader.ReadEntries(10);
                    foreach (var a in entries)
                    {
                        Console.WriteLine($"READ: {a}");
                    }
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine($"Exception while reading endless stream: {e.Message}");
                }

                Task.Delay(1000).Wait();
            }

           
        }


        static void TestWritingAndPerformance(EndlessFileStream fileStream = null)
        {
            var list = fileStream ?? new EndlessFileStream(@"C:\test\FileArray", 1);
            long size = 0;
            Stopwatch w = Stopwatch.StartNew();
            int index = 0;
            for (; ; )
            {
                string entry = Guid.NewGuid().ToString() + ":" + (++index).ToString();
                list.Writer.WriteToFileStream(entry);
                size += entry.Length; // Roughly - string in utf8 might be different to byte in size
                if (w.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine($"{(double)size / (double)w.ElapsedMilliseconds * 1000.0 / 1024.0 / 1024.0} MB/s");
                    Task.Delay(100).Wait();
                    w = Stopwatch.StartNew();
                    size = 0;
                }
            }
        }
        static void Main(string[] args)
        {
            EndlessFileStreamBuilder b = new EndlessFileStreamBuilder();
            //b.GenerateOutputFile(@"C:\test\xlogtest", @"c:\test\central_test.log");
            b.GenerateOutputFile(@"c:\test\logs", @"c:\test\central_test.log");
        

            return;

            //TestWritingAndPerformance();
            ReadWhileWrite();
        }
    }
}
