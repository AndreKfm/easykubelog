using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Scanner.Infrastructure.Adapter.LogDirWatcher.ManualDirectoryScan
{
    public class ManualScanPhysicalFileSystemWatcherFileListSettings
    {
        public ManualScanPhysicalFileSystemWatcherFileListSettings()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Ensure under Windows that we have always the same casing to prevent double 
                // entries which Windows cannot differentiate, but this list would
                NormalizeFileName = s => Path.GetFileName(s).ToLower();
            }
            else
            {
                NormalizeFileName = Path.GetFileName;
            }
        }

        

        public Func<string, string?> NormalizeFileName { get; private set; }
    }
}