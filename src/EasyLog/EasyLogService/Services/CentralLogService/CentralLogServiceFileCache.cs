using EndlessFileStreamClasses;
using LogEntries;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace EasyLogService.Services.CentralLogService
{


    public class FileCache : ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry>
    {

        readonly string _fileName;
        FileStream _file;
        StreamReader _stream;
        IParser _defaultParser = null; 


        public FileCache(string fileName)
        {
            _fileName = fileName;
            _file = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
            _stream = new StreamReader(_file);
        }


        public void Add((DateTimeOffset, int fileIndex) key, KubernetesLogEntry value)
        {

            //if (lines > _maxLines)
            //{

            //}
            //_logCache.Add(key, value);
        }

        public void Dispose()
        {
            _stream?.Dispose(); _stream = null;
            _file?.Dispose(); _file = null;
        }

        IEnumerable<KubernetesLogEntry> EnumerateStreamLines(StreamReader localStream)
        {
            //lock (_lockObject)
            {
                localStream.BaseStream.Seek(0, SeekOrigin.Begin);
                for (; ; )
                {
                    var line = localStream.ReadLine();
                    if (line != null)
                        yield return KubernetesLogEntry.Parse(ref _defaultParser, line);
                    else break;
                }
            }
        }

        private bool CheckInBetween(KubernetesLogEntry k, DateTimeOffset from, DateTimeOffset to)
        {
            var time = k.Time;
            return (from == default || from <= time) && (to == default || to >= time);
        }


        private KubernetesLogEntry[] QueryCaseSensitive(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {

            using FileStream file = File.Open(_fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using StreamReader localStream = new StreamReader(file);
            var result = EnumerateStreamLines(localStream).
                Where(x => CheckInBetween(x, from, to)).
                Where(x => x.Line.Contains(simpleQuery)).
                Take(maxResults).
                OrderByDescending(x => x.Time);
            return result.ToArray();
        }


        private KubernetesLogEntry[] QueryCaseInSensitive(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {
            using FileStream file = File.Open(_fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using StreamReader localStream = new StreamReader(file);
            var result = EnumerateStreamLines(localStream).
                Where(x => CheckInBetween(x, from, to)).
                Where(x => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.Line, simpleQuery, CompareOptions.IgnoreCase) >= 0).
                Take(maxResults).
                OrderByDescending(x => x.Time);
            return result.ToArray();
        }
        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults, CacheQueryMode mode, DateTimeOffset from, DateTimeOffset to)
        {
            if (mode == CacheQueryMode.CaseInsensitive) return QueryCaseInSensitive(simpleQuery, maxResults, from, to);
            return QueryCaseSensitive(simpleQuery, maxResults, from, to);
        }
    }

    public class EndlessFileStreamCache : ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry>
    {
        
        EndlessFileStream _stream;
        IParser _defaultParser; 


        public EndlessFileStreamCache(EndlessFileStream stream)
        {
            _stream = stream;
        }


        public void Dispose()
        {
            _stream?.Dispose(); _stream = null;
        }


        private bool CheckInBetween(KubernetesLogEntry k, DateTimeOffset from, DateTimeOffset to)
        {
            DateTimeOffset time = k.Time;
            return (from == default || from <= time) && (to == default || to >= time);
        }

        private KubernetesLogEntry[] QueryCaseSensitive(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {
            var result = _stream.Reader.ReadEntries(int.MaxValue).
                Where(x => x.content.Contains(simpleQuery)).
                Select(x => KubernetesLogEntry.Parse(ref _defaultParser, x.content, x.filename)).
                Where(x => CheckInBetween(x, from, to)).
                Take(maxResults).
                OrderByDescending(x => x.Time);

            return result.ToArray();
        }
        

        private KubernetesLogEntry[] QueryCaseInSensitive(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {
            var result = _stream.Reader.ReadEntries(int.MaxValue).
              Where(x => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.content, simpleQuery, CompareOptions.IgnoreCase) >= 0).
              Select(x => KubernetesLogEntry.Parse(ref _defaultParser, x.content, x.filename)).
              Where(x => CheckInBetween(x, from, to)).
              Take(maxResults).
              OrderByDescending(x => x.Time);
            return result.ToArray();
        }
        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults, CacheQueryMode mode, DateTimeOffset from, DateTimeOffset to)
        {
            if (mode == CacheQueryMode.CaseInsensitive) return QueryCaseInSensitive(simpleQuery, maxResults, from, to);
            return QueryCaseSensitive(simpleQuery, maxResults, from, to);
        }

        public void Add((DateTimeOffset, int fileIndex) key, KubernetesLogEntry value)
        {
            value.Write(_stream.Writer.WriteToFileStream);
        }
    }

}
