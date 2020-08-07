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


        public FileCache(string fileName)
        {
            _fileName = fileName;
            _file = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
            _stream = new StreamReader(_file);

            // We need to do that dumb counting for the first time to get the number of lines :-/ 
            //while (_stream.ReadLine() != null)
            //{
            //    ++_lines;
            //}
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
                        yield return KubernetesLogEntry.Parse(line);
                    else break;
                }
            }
        }

        private bool CheckInBetween(KubernetesLogEntry k, DateTimeOffset from, DateTimeOffset to)
        {
            return (from == default || from <= k.Time) && (to == default || to >= k.Time);
        }


        private KubernetesLogEntry[] QueryCaseSensitive(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {

            using FileStream file = File.Open(_fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using StreamReader localStream = new StreamReader(file);
            var result = EnumerateStreamLines(localStream).
                Where(x => CheckInBetween(x, from, to)).
                Where(x => x.Log.Contains(simpleQuery)).
                Take(maxResults).
                OrderBy(x => x.Time);
            return result.ToArray();
        }


        private KubernetesLogEntry[] QueryCaseInSensitive(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {
            using FileStream file = File.Open(_fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            using StreamReader localStream = new StreamReader(file);
            var result = EnumerateStreamLines(localStream).
                Where(x => CheckInBetween(x, from, to)).
                Where(x => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.Log, simpleQuery, CompareOptions.IgnoreCase) >= 0).
                Take(maxResults).
                OrderBy(x => x.Time);
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


        public EndlessFileStreamCache(EndlessFileStream stream)
        {
            _stream = stream;

            // We need to do that dumb counting for the first time to get the number of lines :-/ 
        }


        public void Dispose()
        {
            _stream?.Dispose(); _stream = null;
        }

        //IEnumerable<KubernetesLogEntry> EnumerateStreamLines(StreamReader localStream)
        //{
        //    //lock (_lockObject)
        //    {
        //        localStream.BaseStream.Seek(0, SeekOrigin.Begin);
        //        for (; ; )
        //        {
        //            var line = localStream.ReadLine();
        //            if (line != null)
        //                yield return KubernetesLogEntry.Parse(line);
        //            else break;
        //        }
        //    }
        //}

        private bool CheckInBetween(KubernetesLogEntry k, DateTimeOffset from, DateTimeOffset to)
        {
            return (from == default || from <= k.Time) && (to == default || to >= k.Time);
        }

        private KubernetesLogEntry[] QueryCaseSensitive(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {
            var result = _stream.Reader.ReadEntries(int.MaxValue).
                Where(x => x.content.Contains(simpleQuery)).
                Select(x => KubernetesLogEntry.Parse(x.content, x.filename)).
                Where(x => CheckInBetween(x, from, to)).
                Take(maxResults);
            return result.ToArray();
        }
        

        private KubernetesLogEntry[] QueryCaseInSensitive(string simpleQuery, int maxResults, DateTimeOffset from, DateTimeOffset to)
        {
            var result = _stream.Reader.ReadEntries(int.MaxValue).
              Where(x => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.content, simpleQuery, CompareOptions.IgnoreCase) >= 0).
              Select(x => KubernetesLogEntry.Parse(x.content, x.filename)).
              Where(x => CheckInBetween(x, from, to)).
              Take(maxResults);
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
