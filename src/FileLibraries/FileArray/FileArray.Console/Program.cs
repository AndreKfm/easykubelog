using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;

namespace FileArrayConsole
{

    class EndlessStreamFileListEntry
    {
        public string FileName { get; set; }
        public long FileIndex { get; set; }
    }

    class EndlessStreamFileList
    {
        private int _maxEntries;
        private string _baseDirectory;

        public EndlessStreamFileList(int maxEntries, string baseDirectory)
        {
            _maxEntries = maxEntries;
            _baseDirectory = baseDirectory;
            ReadFromFile();
        }

        string IndexListFileName => $"{_baseDirectory}/index.txt";

        string GenerateNewFileName => $"{_baseDirectory}/endless-{Guid.NewGuid()}.log";

        void ReadFromFile()
        {
            if (!File.Exists(IndexListFileName))
                return; 

            var lines = File.ReadAllLines(IndexListFileName);
            var builder = _fileList.ToBuilder();

            foreach (var line in lines)
            {
                var entry = JsonSerializer.Deserialize<EndlessStreamFileListEntry>(line);
                builder.Add(entry.FileIndex, entry);
            }
            _fileList = builder.ToImmutable();
        }

        void WriteToFile()
        {
            List<string> stringList = new List<string>();

            var list = _fileList;
            foreach (var entry in list)
            {
                string line = JsonSerializer.Serialize<EndlessStreamFileListEntry>(entry.Value);
                stringList.Add(line);
            }
            File.WriteAllLines(IndexListFileName, stringList) ;
        }

        public void AddNewFileDeleteOldestIfNeeded()
        {
            var list = _fileList;
            long newIndex = 0;
            if (list.Count > 0)
            {
                var last = list.Values.Last();
                newIndex = last.FileIndex + 1;
            }
            list = list.Add(newIndex, new EndlessStreamFileListEntry { FileName = GenerateNewFileName, FileIndex = newIndex });

            if (list.Count > _maxEntries)
            {
                list = list.Remove(list.Values.First().FileIndex);
            }

            _fileList = list;
            WriteToFile();
        }


        ImmutableSortedDictionary<long, EndlessStreamFileListEntry> _fileList = ImmutableSortedDictionary<long, EndlessStreamFileListEntry>.Empty;
    }

    class Program
    {
        static void Main(string[] args)
        {
            EndlessStreamFileList list = new EndlessStreamFileList(4, @"C:\test\FileArray");
            for (; ;)  list.AddNewFileDeleteOldestIfNeeded();
        }
    }
}
