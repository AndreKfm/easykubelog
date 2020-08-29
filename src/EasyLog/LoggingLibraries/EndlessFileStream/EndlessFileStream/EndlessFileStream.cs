using FileToolsClasses;
using LogEntries;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EndlessFileStreamClasses
{
    using FileListType = ImmutableSortedDictionary<long, EndlessStreamFileListEntry>;

    class EndlessFileStreamFileOperations : IEndlessFileStreamFileListOperations
    {
        readonly IEndlessFileStreamNames _fileNames;

        public EndlessFileStreamFileOperations(IEndlessFileStreamNames fileNames)
        {
            _fileNames = fileNames;
        }


        public void WriteListToFile(FileListType fileList)
        {
            List<string> stringList = new List<string>();

            var list = fileList;
            foreach (var entry in list)
            {
                entry.Value.FileName = Path.GetFullPath(entry.Value.FileName);
                string line = JsonSerializer.Serialize<EndlessStreamFileListEntry>(entry.Value);
                stringList.Add(line);
            }

            var hash = CalcHashCodeString(stringList);
            File.WriteAllText(_fileNames.IndexHashFileName, hash);

            File.WriteAllLines(_fileNames.IndexListFileName, stringList);
            PurgeRedundantFiles(fileList);
        }

        private static string GetShaHash(string stringTobeHashed)
        {
            if (String.IsNullOrEmpty(stringTobeHashed))
            {
                stringTobeHashed = "#"; // always return a hash
            }

            using var shaManaged = new System.Security.Cryptography.SHA256Managed();
            byte[] textData = System.Text.Encoding.UTF8.GetBytes(stringTobeHashed);
            byte[] hash = shaManaged.ComputeHash(textData);
            return BitConverter.ToString(hash);
        }


        /// <summary>
        /// Really simple and enough to check integrety of index file
        /// </summary>
        /// <param name="stringList"></param>
        /// <returns></returns>
        static string CalcHashCodeString(List<string> stringList)
        {
            string toBeHashed = String.Join(',', stringList.ToArray());
            return GetShaHash(toBeHashed);
        }


        public FileListType ReadListFromFile()
        {

            if (!File.Exists(_fileNames.IndexListFileName))
                return null;

            var lines = File.ReadAllLines(_fileNames.IndexListFileName);
            var calculatedString = CalcHashCodeString(lines.ToList());

            var origHash = File.Exists(_fileNames.IndexHashFileName) ? File.ReadAllText(_fileNames.IndexHashFileName) : String.Empty;
            if (calculatedString != origHash)
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

        static public IEnumerable<(string filename, string content)> ReadFromFileStream(string fileName,
                                                             Func<string, FileStream> OpenFile, long maxLines = long.MaxValue)
        {
            using FileStream fileStream = OpenFile(fileName);
            using StreamReader reader = new StreamReader(fileStream);
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


                if (line != null && --maxLines > 0)
                    yield return (Path.GetFileNameWithoutExtension(fileName), line);
                else break;
            }
        }

        static public IEnumerable<(string filename, string content)> ReadFromFileStreamBackwards(string fileName,
                                                             Func<string, FileStream> OpenFile, long maxLines = long.MaxValue)
        {
            FileSeeker f = new FileSeeker();
            using var fileStream = new FileStreamWrapper(OpenFile(fileName));
            fileStream.Seek(0, SeekOrigin.End); // We want to read backwards - so start at the end
            for (; ; )
            {
                string line = null;
                try
                {
                    line = f.SeekLastLineFromCurrentAndPositionOnStartOfItAndReturnReadLine(fileStream);
                }
                catch (Exception)
                {
                }

                if (line != null && --maxLines > 0)
                    yield return (Path.GetFileNameWithoutExtension(fileName), line);
                else break;
            }
        }

        static public IEnumerable<(string filename, string content)> ReadFromFileStream(string[] listToRead, long maxLines, Func<string, FileStream> OpenFile)
        {

            foreach (var file in listToRead)
            {
                foreach (var line in ReadFromFileStream(file, OpenFile, maxLines))
                {
                    yield return line;
                }
            }
        }

        static public IEnumerable<(string filename, string content)> ReadFromFileStreamBackwards(string[] listToRead, long maxLines, Func<string, FileStream> OpenFile)
        {

            foreach (var file in listToRead)
            {
                foreach (var line in ReadFromFileStreamBackwards(file, OpenFile, maxLines))
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
        private readonly int _maxEntries;
        private readonly IEndlessFileStreamFileListOperations _fileOperations;
        private readonly IEndlessFileStreamNames _fileNames;


        public EndlessFileStreamFileList(int maxEntries,
                                         string baseDirectory,
                                         IEndlessFileStreamFileListOperations fileOperations = null,
                                         IEndlessFileStreamNames fileNames = null)
        {
            _maxEntries = maxEntries;
            _fileNames = fileNames ?? new EndlessFileStreamNames(baseDirectory);
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
                    catch (Exception)
                    {
                        // might happen if the file is currently read and cannot be deleted
                    }
                    list = list.Remove(toDelete.FileIndex);
                }
            }
            catch (Exception e)
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
        public IEnumerable<(string filename, string content)> ReadFromFileStream(int maxLines);
        public IEnumerable<(string filename, string content)> ReadFromFileStreamBackwards(int maxLines);

        public Task Flush();

    }

    public class EndlessFileStreamIO : IEndlessFileStreamIO
    {
        private readonly string _baseDirectory;
        private readonly IEndlessFileStreamFileList _fileList;
        private readonly IEndlessFileStreamNames _fileNames;
        private readonly long _maxLogFileSizeInBytesEachFile;
        private FileStream _fileStream;
        private StreamWriter _writer;
        private readonly IEndlessFileStreamFileListOperations _fileOperations;

        public EndlessFileStreamIO(string baseDirectory,
                                   long maxLogFileSizeInMBytes = 1024,
                                   long maxLogFileSizeInKByte = 0,
                                   int splitIntoCountFiles = 4,
                                   IEndlessFileStreamFileList fileList = null,
                                   IEndlessFileStreamFileListOperations fileOperations = null,
                                   IEndlessFileStreamNames fileNames = null)
        {
            _baseDirectory = baseDirectory;

            try
            {
                if (!Directory.Exists(_baseDirectory))
                    Directory.CreateDirectory(_baseDirectory);
            }
            catch (Exception)
            {

            }


            _fileNames = fileNames ?? new EndlessFileStreamNames(baseDirectory);
            _fileOperations = fileOperations ?? new EndlessFileStreamFileOperations(_fileNames);
            _fileList = fileList ?? new EndlessFileStreamFileList(splitIntoCountFiles, _baseDirectory, _fileOperations, _fileNames);
            if (splitIntoCountFiles <= 0)
                throw new ArgumentException($"Invalid number of files to split logfile into: {splitIntoCountFiles}");
            _maxLogFileSizeInBytesEachFile = (maxLogFileSizeInMBytes * 1024 * 1024 + maxLogFileSizeInKByte) / splitIntoCountFiles;
        }

        static void DisposeAndSetToNull<T>(ref T stream) where T : IDisposable
        {
            try
            {
                stream?.Dispose();
            }
            catch (Exception)
            {
            }
            stream = default;
        }

        FileStream Open(string fileName)
        {
            return File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, (FileShare)(FileShare.ReadWrite | FileShare.Delete));
        }

        FileStream OpenWriteOnly(string fileName)
        {
            return File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write, (FileShare)(FileShare.ReadWrite | FileShare.Delete));
        }

        void InitCurrentFile()
        {
            var nameOfLatestFile = _fileList.GetLatestEntryFileName();
            Close();
            _fileStream = OpenWriteOnly(nameOfLatestFile);
            _fileStream.Seek(0, SeekOrigin.End); // Append to end of file
            _writer = new StreamWriter(_fileStream);

        }

        void Close()
        {
            DisposeAndSetToNull(ref _writer);
            DisposeAndSetToNull(ref _fileStream);
        }


        public async Task Flush()
        {
            await Writer().FlushAsync();
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
            await Writer().FlushAsync();
        }

        public IEnumerable<(string filename, string content)> ReadFromFileStream(int maxLines)

        {
            var listToRead = _fileList.GetFileList();
            var fileList = listToRead.Select(x => x.Value.FileName).ToArray();
            return FileHelper.ReadFromFileStream(fileList, maxLines, this.Open);
        }

        public IEnumerable<(string filename, string content)> ReadFromFileStreamBackwards(int maxLines)

        {
            var listToRead = _fileList.GetFileList();
            var fileList = listToRead.Select(x => x.Value.FileName).Reverse().ToArray();
            return FileHelper.ReadFromFileStreamBackwards(fileList, maxLines, this.Open);
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
        public IEnumerable<(string filename, string content)> ReadEntries(FileStreamDirection direction, int maxLines = 100);
    }



    public class EndlessFileStreamReader : IEndlessFileStreamReader
    {
        readonly IEndlessFileStreamIO _fileIO;

        public EndlessFileStreamReader(IEndlessFileStreamIO fileIO)
        {
            _fileIO = fileIO;
        }


        public IEnumerable<(string filename, string content)> ReadEntries(FileStreamDirection direction, int maxLines = 100)
        {
            if (direction == FileStreamDirection.Backwards)
                return _fileIO.ReadFromFileStreamBackwards(maxLines).Take(maxLines);
            return _fileIO.ReadFromFileStream(maxLines).Take(maxLines);
        }
    }

    public interface IEndlessFileStreamWriter
    {
        public Task WriteToFileStream(string line);
        public void Flush();
    }


    public class EndlessFileStreamWriter : IEndlessFileStreamWriter
    {
        readonly IEndlessFileStreamIO _fileIO;

        public EndlessFileStreamWriter(IEndlessFileStreamIO fileIO)
        {
            _fileIO = fileIO;
        }


        public async Task WriteToFileStream(string line)
        {
            await _fileIO.WriteToFileStream(line);
        }

        public void Flush()
        {
            _fileIO.Flush().Wait(); ;
        }
    }

    public class EndlessFileStreamSettings
    {

        public string BaseDirectory { get; set; }
        public long MaxLogFileSizeInMByte { get; set; } = 1024;
        public int NumberOfLogFilesToUseForCentralDatabase { get; set; } = 4;
        public int MaxLogFileSizeInKByte { get; set; } = 0;
    }

    public class EndlessFileStream : IDisposable
    {
        IEndlessFileStreamWriter _writer;
        IEndlessFileStreamReader _reader;
        IEndlessFileStreamIO _io;
        EndlessFileStreamSettings _settings;

        public EndlessFileStream(EndlessFileStreamSettings settings,
                                 IEndlessFileStreamWriter writer = null,
                                 IEndlessFileStreamReader reader = null,
                                 IEndlessFileStreamIO io = null)
        {
            _settings = settings;
            _io = io ?? new EndlessFileStreamIO(settings.BaseDirectory,
                                                settings.MaxLogFileSizeInMByte,
                                                settings.MaxLogFileSizeInKByte,
                                                settings.NumberOfLogFilesToUseForCentralDatabase,
                                                null, null, null);
            _writer = writer ?? new EndlessFileStreamWriter(_io);
            _reader = reader ?? new EndlessFileStreamReader(_io);
        }

        public IEndlessFileStreamWriter Writer { get { return _writer; } }
        public IEndlessFileStreamReader Reader { get { return _reader; } }

        public void Dispose()
        {
            _io?.Dispose();
            _io = null;
            _reader = null;
            _writer = null;
        }
    }


    /// <summary>
    /// Helper class to build a central log file - can be used either for testing or on startup to generate a log file 
    /// from already existing logs
    /// </summary>
    public class EndlessFileStreamBuilder
    {

        IParser _defaultParser;

        public EndlessFileStreamBuilder()
        {

        }

        public IEnumerable<KubernetesLogEntry> EnumerateSortedByDateTime(IEnumerable<IEnumerable<(string filename, string content)>> fileListEnumerationWithFileEntries)
        {
            List<IEnumerator<(string filename, string content)>> iteratorList = new List<IEnumerator<(string filename, string content)>>();

            foreach (var fileEnum in fileListEnumerationWithFileEntries)
            {
                var iterator = fileEnum.GetEnumerator();
                iterator.MoveNext();
                iteratorList.Add(iterator);
            }



            ConcurrentQueue<(long ticks, IEnumerator<(string filename, string content)> iterator, KubernetesLogEntry k)> queue =
                new ConcurrentQueue<(long ticks, IEnumerator<(string filename, string content)> iterator, KubernetesLogEntry k)>();
            for (; ; )
            {
                var result = Parallel.ForEach(iteratorList, (iterator) =>
                {
                    if (String.IsNullOrEmpty(iterator.Current.content) == false)
                    {
                        var k = KubernetesLogEntry.Parse(ref _defaultParser, iterator.Current.content);


                        if ((k != null) && (!k.IsDefault())) // Just to play it safe - remove empty lines
                        {
                            k.SetContainerName(iterator.Current.filename);
                            long ticks = k.Time.Ticks;

                            queue.Enqueue((ticks, iterator, k));
                            //sortList.TryAdd(ticks, (iterator, k));
                        }
                    }
                });



                long currentTick = long.MaxValue;
                IEnumerator<(string filename, string content)> iterator = null;
                KubernetesLogEntry k = null;
                while (queue.TryDequeue(out var i))
                {
                    if (i.ticks < currentTick)
                    {
                        currentTick = i.ticks;
                        iterator = i.iterator;
                        k = i.k;
                    }
                }

                if (k == null || iterator == null)
                    break;


                if (iterator.MoveNext() == false)
                {
                    iteratorList.Remove(iterator);
                }

                yield return k;
            }
        }


        public void GenerateOutputFile(string baseDirectory, string outputFile)
        {
            List<IEnumerable<(string filename, string content)>> streams = OpenFiles(baseDirectory);

            var listEnumerator = streams.AsEnumerable();

            using var fileStream = FileHelper.DefaultOpenFileCreate(outputFile);
            using var writer = new StreamWriter(fileStream);
            foreach (var s in EnumerateSortedByDateTime(listEnumerator))
            {
                //if (s.IsDefault == false)
                s.Write(writer);
            }
        }

        public void GenerateEndlessFileStream(EndlessFileStreamSettings settings, string sourceDirectory)
        {
            List<IEnumerable<(string filename, string content)>> streams = OpenFiles(sourceDirectory);

            var listEnumerator = streams.AsEnumerable();

            using EndlessFileStream file = new EndlessFileStream(settings);
            foreach (var s in EnumerateSortedByDateTime(listEnumerator))
            {
                s.Write((string line) =>
                {
                    file.Writer.WriteToFileStream(line).Wait(); // It's ok right now here to block 
                    file.Writer.Flush();
                    return Task.CompletedTask;
                });
            }
        }

        void AddFilesInDirectory(string baseDirectory, List<IEnumerable<(string filename, string content)>> streams)
        {
            var files = Directory.GetFiles(baseDirectory);
            foreach (var file in files)
            {
                streams.Add(FileHelper.ReadFromFileStream(file, (string file) => { return File.OpenRead(file); }));
            }
        }

        void InternalOpenFiles(string baseDirectory, List<IEnumerable<(string filename, string content)>> streams)
        {
            AddFilesInDirectory(baseDirectory, streams);
            var dirs = Directory.GetDirectories(baseDirectory);
            foreach (var dir in dirs)
            {
                InternalOpenFiles(dir, streams);
            }
        }


        List<IEnumerable<(string filename, string content)>> OpenFiles(string baseDirectory)
        {
            List<IEnumerable<(string filename, string content)>> streams = new List<IEnumerable<(string filename, string content)>>();
            InternalOpenFiles(baseDirectory, streams);
            return streams;
        }


    }
}
