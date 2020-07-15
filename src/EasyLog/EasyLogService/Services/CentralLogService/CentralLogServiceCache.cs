using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using C5;

namespace EasyLogService.Services.CentralLogService
{
    public class CentralLogServiceCache : ICentralLogServiceCache
    {
        readonly int _maxLines;
        public CentralLogServiceCache(int maxLines)
        {
            _maxLines = maxLines;
        }

        readonly Dictionary<string, int> _fileIndexList = new Dictionary<string, int>();
        int _currentFileIndex = 0;

        readonly TreeDictionary<(DateTimeOffset time, int fileIndex), KubernetesLogEntry> _logCache = new TreeDictionary<(DateTimeOffset time, int fileIndex), KubernetesLogEntry>();
        public void AddEntry(LogEntry entry)
        {
            var lines = entry.Lines.Split('\n');
            foreach (string line in lines)
            {
                if (line.Length > 0)
                {
                    var newEntry = KubernetesLogEntry.Parse(line);
                    if (!newEntry.IsDefault)
                    {
                        lock (_logCache)
                        {
                            if (!_fileIndexList.TryGetValue(entry.FileName, out int fileIndex))
                            {
                                fileIndex = ++_currentFileIndex;
                                _fileIndexList.Add(entry.FileName, fileIndex);
                            }
                            if (_logCache.Count > _maxLines)
                                _logCache.Remove(_logCache.First().Key);
                            try
                            {
                                _logCache.Add((newEntry.Time, fileIndex), newEntry);
                            }
                            catch (Exception)
                            { }
                        }
                    }
                }
            }
        }


        public KubernetesLogEntry[] Query(string simpleQuery, int maxResults)
        {
            lock (_logCache)
            {
                var result = _logCache.AsParallel().
                    Where(x => x.Value.Log.Contains(simpleQuery)).
                    Take(maxResults).
                    Select(x => x.Value).
                    OrderBy(x => x.Time);
                //var result = _logCache.Where(x => x.Value.log.Contains(simpleQuery)).Select(x => x.Value);
                return result.ToArray();
            }
        }

        public void Dispose()
        {
            _logCache.Clear();
        }
    }
}
