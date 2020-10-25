using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Scanner.Domain.Shared;

namespace Scanner.Domain.Ports
{
    public interface IDirectoryFileScanner
    {
        public string GetCurrentDirectory();
        public void ScanDirectory();
        public ReadOnlyCollection<FileEntry> GetChangedFiles();
    }
}
