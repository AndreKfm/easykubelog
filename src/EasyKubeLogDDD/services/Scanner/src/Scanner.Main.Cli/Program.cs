using System;
using Scanner.Domain;
using Scanner.Domain.Ports;
using Scanner.Infrastructure.Adapter;
using Scanner.Infrastructure.Adapter.LogDirWatcher;
using Scanner.Infrastructure.Adapter.LogDirWatcher.ManualDirectoryScan;
using Scanner.Infrastructure.Adapter.ScanLogFiles;

namespace Scanner.Main.Cli
{



    class Program
    {
        static void Main(string[] args)
        {
            ManualDirectoryScanAndGenerateDifferenceToLastScan pollDirectoryForChanges = 
                new ManualDirectoryScanAndGenerateDifferenceToLastScan(new ManualDirectoryScanAndGenerateDifferenceToLastScanSettings(@"d:\test\polldir"),
                    new ManualScanDirectory());

            LogDirectoryWatcher watcher = new LogDirectoryWatcher(pollDirectoryForChanges);

            ScanLogFile scanner = new ScanLogFile();

            ScannerMain main = new ScannerMain(watcher, scanner);

            Console.WriteLine("Starting scanner");
            main.Start();
            Console.ReadKey();
            Console.WriteLine("Stopping scanner");
            main.Stop();
            Console.WriteLine("Stopped scanner - exit");
        }
    }
}
