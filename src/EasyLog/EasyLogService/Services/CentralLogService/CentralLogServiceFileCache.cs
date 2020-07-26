using EndlessFileStreamClasses;
using LogEntries;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EasyLogService.Services.CentralLogService
{


    public class FileCache : ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry>
    {
        int _maxLines;

        string _fileName;
        FileStream _file;
        StreamReader _stream;
        long _lines = 0;

        Object _lockObject = new object();

        public FileCache(string fileName, int maxLines)
        {
            _fileName = fileName;
            _maxLines = maxLines;
            _file = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            _stream = new StreamReader(_file);

            // We need to do that dumb counting for the first time to get the number of lines :-/ 
            while (_stream.ReadLine() != null)
            {
                ++_lines;
            }
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


        private KubernetesLogEntry[] QueryCaseSensitive(string simpleQuery, int maxResults)
        {

            using (FileStream file = File.Open(_fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamReader localStream = new StreamReader(file))
            {
                var result = EnumerateStreamLines(localStream).
                Where(x => x.Log.Contains(simpleQuery)).
                Take(maxResults).
                OrderBy(x => x.Time);
                return result.ToArray();
            }
        }


        private KubernetesLogEntry[] QueryCaseInSensitive(string simpleQuery, int maxResults)
        {
            using (FileStream file = File.Open(_fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamReader localStream = new StreamReader(file))
            {
                var result = EnumerateStreamLines(localStream).
                    Where(x => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.Log, simpleQuery, CompareOptions.IgnoreCase) >= 0).
                    Take(maxResults).
                    OrderBy(x => x.Time);
                return result.ToArray();
            }
        }
        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults, CacheQueryMode mode)
        {
            if (mode == CacheQueryMode.CaseInsensitive) return QueryCaseInSensitive(simpleQuery, maxResults);
            return QueryCaseSensitive(simpleQuery, maxResults);
        }
    }

    public class EndlessFileStreamCache : ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry>
    {
        
        EndlessFileStream _stream;


        public EndlessFileStreamCache(EndlessFileStream stream, int maxLines)
        {
            _stream = stream;

            // We need to do that dumb counting for the first time to get the number of lines :-/ 
        }


        public void Dispose()
        {
            _stream?.Dispose(); _stream = null;
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


        private KubernetesLogEntry[] QueryCaseSensitive(string simpleQuery, int maxResults)
        {
            var result = _stream.Reader.ReadEntries(maxResults).
            Where(x => x.Contains(simpleQuery)).
            Take(maxResults).Select(x => KubernetesLogEntry.Parse(x));
            return result.ToArray();
        }


        private KubernetesLogEntry[] QueryCaseInSensitive(string simpleQuery, int maxResults)
        {
            var result = _stream.Reader.ReadEntries(maxResults).
            Where(x => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x, simpleQuery, CompareOptions.IgnoreCase) >= 0).
            Take(maxResults).Select(x => KubernetesLogEntry.Parse(x));
            return result.ToArray();
        }
        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults, CacheQueryMode mode)
        {
            if (mode == CacheQueryMode.CaseInsensitive) return QueryCaseInSensitive(simpleQuery, maxResults);
            return QueryCaseSensitive(simpleQuery, maxResults);
        }

        public void Add((DateTimeOffset, int fileIndex) key, KubernetesLogEntry value)
        {
            throw new NotImplementedException();
        }
    }

}
