using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Scanner.Domain.Ports;
using Scanner.Domain.Shared;

namespace Scanner.Infrastructure.Adapter.LogDirWatcher
{
    public class LogDirectoryWatcher : ILogDirWatcher
    {
        private readonly IDirectoryFileScanner _pollNewFiles;


        public LogDirectoryWatcher(IDirectoryFileScanner pollNewFiles)
        {
            _pollNewFiles = pollNewFiles;
        }


        public void ScanDirectory()
        {
            _pollNewFiles.ScanDirectory();
        }



        public ReadOnlyCollection<FileEntry> GetChangedFiles()
        {
            return _pollNewFiles.GetChangedFiles();
        }

    }
}
