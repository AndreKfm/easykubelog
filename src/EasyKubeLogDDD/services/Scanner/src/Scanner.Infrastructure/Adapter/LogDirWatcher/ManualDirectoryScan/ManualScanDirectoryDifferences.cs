using System.Collections.Generic;
using System.Linq;

namespace Scanner.Infrastructure.Adapter.LogDirWatcher.ManualDirectoryScan
{
    public class ManualScanDirectoryDifferences
    {
        public IEnumerable<KeyValuePair<string, long>> GetNewFiles(Dictionary<string, long> oldScanned, Dictionary<string, long> newScanned)
        {
            return newScanned.Where(s => oldScanned.ContainsKey(s.Key) == false);
        }

        public IEnumerable<KeyValuePair<string, long>> GetDeletedFiles(Dictionary<string, long> oldScanned, Dictionary<string, long> newScanned)
        {
            return oldScanned.Where(s => newScanned.ContainsKey(s.Key) == false);
        }

        public IEnumerable<KeyValuePair<string, long>> GetChangedFiles(Dictionary<string, long> oldScanned, Dictionary<string, long> newScanned)
        {
            return newScanned.Where(s => oldScanned.ContainsKey(s.Key) && (oldScanned[s.Key] != s.Value));
        }
    }
}