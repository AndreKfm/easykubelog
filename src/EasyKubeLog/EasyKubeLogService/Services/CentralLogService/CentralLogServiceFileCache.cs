using FileToolsClasses;
using LogEntries;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EasyKubeLogService.Services.CentralLogService
{


    // ReSharper disable once UnusedMember.Global
    public class FileCache : ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry>
    {
        private readonly string _fileName;
        private StreamWriter _streamWriter;
        private FileStream _file;
        private IParser _defaultParser;

        public FileCache(string fileName)
        {
            _fileName = fileName;
            _file = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
            _streamWriter = new StreamWriter(_file);
        }

        public void Add((DateTimeOffset, int fileIndex) key, KubernetesLogEntry value)
        {
            _streamWriter.Write(value);
        }

        public void Flush()
        {
            _streamWriter.Flush();
        }

        public void Dispose()
        {
            _streamWriter?.Dispose(); _streamWriter = null;
            _file?.Dispose(); _file = null;
        }

        private IEnumerable<KubernetesLogEntry> EnumerateStreamLines(IStreamReaderWrapper localStream)
        {
            //lock (_lockObject)
            {
                localStream.Seek(0, SeekOrigin.Begin);
                for (; ; )
                {
                    var line = localStream.ReadLine();
                    if (line != null)
                        yield return KubernetesLogEntry.Parse(ref _defaultParser, line);
                    else break;
                }
            }
        }

        private bool CheckInBetween(KubernetesLogEntry k, TimeRange timeRange)
        {
            return timeRange.IsInBetweenOrDefault(k.Time);
        }

        private IStreamReaderWrapper CreateStreamReader()
        {
            StreamReaderWrapperFactoryImpl streamReaderFactory = new StreamReaderWrapperFactoryImpl(_fileName, new FileModes(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            return streamReaderFactory.Create();
        }

        private KubernetesLogEntry[] LocalQuery(QueryParams queryParams, Func<KubernetesLogEntry, bool> compareFunction)
        {
            using IStreamReaderWrapper localStream = CreateStreamReader();
            var result = EnumerateStreamLines(localStream).
                Where(x => CheckInBetween(x, queryParams.Time)).
                Where(compareFunction).
                Take(queryParams.MaxResults).
                OrderByDescending(x => x.Time);
            return result.ToArray();
        }
        
        private KubernetesLogEntry[] CaseInSensitiveQuery(QueryParams queryParams)
        {
            bool Compare(KubernetesLogEntry k) => CultureInfo.CurrentCulture.CompareInfo.IndexOf(k.Line, queryParams.SimpleQuery, CompareOptions.IgnoreCase) >= 0;
            return LocalQuery(queryParams, Compare);
        }
        private KubernetesLogEntry[] CaseSensitiveQuery(QueryParams queryParams)
        {
            bool Compare(KubernetesLogEntry k) => k.Line.Contains(queryParams.SimpleQuery);
            return LocalQuery(queryParams, Compare);
        }

        public KubernetesLogEntry[] Query(QueryParams queryParams, CacheQueryMode mode)
        {
            if (mode == CacheQueryMode.CaseInsensitive) return CaseInSensitiveQuery(queryParams);
            return CaseSensitiveQuery(queryParams);
        }
    }


    public class EndlessFileStreamCache : ICache<(DateTimeOffset, int fileIndex), KubernetesLogEntry>
    {
        private EndlessFileStream.EndlessFileStream _stream;
        private IParser _defaultParser;

        private readonly FileStreamDirection _direction;

        public EndlessFileStreamCache(EndlessFileStream.EndlessFileStream stream, FileStreamDirection direction = FileStreamDirection.Backwards)
        {
            _stream = stream;
            _direction = direction;
        }

        public void Dispose()
        {
            _stream?.Dispose(); 
            _stream = null;
        }

        private bool CheckInBetween(KubernetesLogEntry k, TimeRange timeRange)
        {
            return timeRange.IsInBetweenOrDefault(k.Time);
        }

        private KubernetesLogEntry[] QueryCaseSensitive(QueryParams queryParams)
        {
            var result = _stream.Reader.ReadEntries(_direction, int.MaxValue).
                Where(x => x.content != null && x.content.Contains(queryParams.SimpleQuery)).
                Select(x => KubernetesLogEntry.Parse(ref _defaultParser, x.content, x.filename)).
                Where(x => CheckInBetween(x, queryParams.Time)).
                Take(queryParams.MaxResults).
                OrderByDescending(x => x.Time);

            return result.ToArray();
        }

        private KubernetesLogEntry[] QueryCaseInSensitive(QueryParams queryParams)
        {
            var result = _stream.Reader.ReadEntries(_direction, int.MaxValue).
              Where(x => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.content ?? string.Empty, queryParams.SimpleQuery, CompareOptions.IgnoreCase) >= 0).
              Select(x => KubernetesLogEntry.Parse(ref _defaultParser, x.content, x.filename)).
              Where(x => CheckInBetween(x, queryParams.Time)).
              Take(queryParams.MaxResults).
              OrderByDescending(x => x.Time);
            return result.ToArray();
        }

        public KubernetesLogEntry[] Query(QueryParams queryParams, CacheQueryMode mode)
        {
            if (mode == CacheQueryMode.CaseInsensitive) return QueryCaseInSensitive(queryParams);
            return QueryCaseSensitive(queryParams);
        }

        public void Add((DateTimeOffset, int fileIndex) key, KubernetesLogEntry value)
        {
            value.Write(_stream.Writer.WriteToFileStream);
        }

        public void Flush()
        {
            _stream?.Writer?.Flush();
        }
    }
}