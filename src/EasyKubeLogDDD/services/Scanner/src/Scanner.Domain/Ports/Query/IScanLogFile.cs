using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Scanner.Domain.Entities;
using Scanner.Domain.Events;
using Scanner.Domain.Shared;

namespace Scanner.Domain.Ports.Query
{
    public interface IScanLogFile
    {
        public void ScanLogFiles(ReadOnlyCollection<FileEntry> fileChanges);
        public IReadOnlyCollection<(string name, IReadOnlyCollection<LogEntry> logEntries)> GetChanges();
    }
}
