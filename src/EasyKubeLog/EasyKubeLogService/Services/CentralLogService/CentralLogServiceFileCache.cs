﻿using FileToolsClasses;
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

        private IEnumerable<KubernetesLogEntry> EnumerateStreamLines(StreamReader localStream)
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

        private bool CheckInBetween(KubernetesLogEntry k, TimeRange timeRange)
        {
            return timeRange.IsInBetweenOrDefault(k.Time);
        }


        private StreamReader CreateStreamReader()
        {
            FileStream file = File.Open(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader localStream = new StreamReader(file);
            return localStream;
        }



        private KubernetesLogEntry[] Query(string simpleQuery, int maxResults, TimeRange timeRange, Func<KubernetesLogEntry, bool> compareFunction)
        {
            using StreamReader localStream = CreateStreamReader();
            var result = EnumerateStreamLines(localStream).
                Where(x => CheckInBetween(x, timeRange)).
                Where(compareFunction).
                Take(maxResults).
                OrderByDescending(x => x.Time);
            return result.ToArray();
        }
        
        private KubernetesLogEntry[] CaseInSensitiveQuery(string simpleQuery, int maxResults, TimeRange timeRange)
        {
            bool Compare(KubernetesLogEntry k) => CultureInfo.CurrentCulture.CompareInfo.IndexOf(k.Line, simpleQuery, CompareOptions.IgnoreCase) >= 0;
            return Query(simpleQuery, maxResults, timeRange, Compare);
        }
        private KubernetesLogEntry[] CaseSensitiveQuery(string simpleQuery, int maxResults, TimeRange timeRange)
        {
            bool Compare(KubernetesLogEntry k) => k.Line.Contains(simpleQuery);
            return Query(simpleQuery, maxResults, timeRange, Compare);
        }

        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults, CacheQueryMode mode, TimeRange timeRange)
        {
            if (mode == CacheQueryMode.CaseInsensitive) return CaseInSensitiveQuery(simpleQuery, maxResults, timeRange);
            return CaseSensitiveQuery(simpleQuery, maxResults, timeRange);
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

        private KubernetesLogEntry[] QueryCaseSensitive(string simpleQuery, int maxResults, TimeRange timeRange)
        {
            var result = _stream.Reader.ReadEntries(_direction, int.MaxValue).
                Where(x => x.content != null && x.content.Contains(simpleQuery)).
                Select(x => KubernetesLogEntry.Parse(ref _defaultParser, x.content, x.filename)).
                Where(x => CheckInBetween(x, timeRange)).
                Take(maxResults).
                OrderByDescending(x => x.Time);

            return result.ToArray();
        }

        private KubernetesLogEntry[] QueryCaseInSensitive(string simpleQuery, int maxResults, TimeRange timeRange)
        {
            var result = _stream.Reader.ReadEntries(_direction, int.MaxValue).
              Where(x => CultureInfo.CurrentCulture.CompareInfo.IndexOf(x.content ?? string.Empty, simpleQuery, CompareOptions.IgnoreCase) >= 0).
              Select(x => KubernetesLogEntry.Parse(ref _defaultParser, x.content, x.filename)).
              Where(x => CheckInBetween(x, timeRange)).
              Take(maxResults).
              OrderByDescending(x => x.Time);
            return result.ToArray();
        }

        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults, CacheQueryMode mode, TimeRange timeRange)
        {
            if (mode == CacheQueryMode.CaseInsensitive) return QueryCaseInSensitive(simpleQuery, maxResults, timeRange);
            return QueryCaseSensitive(simpleQuery, maxResults, timeRange);
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