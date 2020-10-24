using System;
using Scanner.Domain;
using Scanner.Domain.Ports;
using Scanner.Infrastructure.Adapter;
using Scanner.Infrastructure.Adapter.LogDirWatcher;
using Scanner.Infrastructure.Adapter.LogDirWatcher.ManualDirectoryScan;

namespace Scanner.Main.Cli
{


    class LogFileChangedHandler : ILogFileChanged
    {
        public void LogFileChanged(string logFilePath)
        {
            Console.WriteLine($"Log file changed: {logFilePath}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ManualDirectoryScanAndGenerateDifferenceToLastScan pollDirectoryForChanges = 
                new ManualDirectoryScanAndGenerateDifferenceToLastScan(new ManualDirectoryScanAndGenerateDifferenceToLastScanSettings(@"d:\test\polldir"),
                    new ManualScanDirectory());

            LogFileChangedHandler handler = new LogFileChangedHandler();
            LogDirectoryWatcher watcher = new LogDirectoryWatcher(pollDirectoryForChanges);

            ScannerMain main = new ScannerMain(watcher, handler);

            Console.WriteLine("Starting scanner");
            main.Start(handler);
            Console.ReadKey();
            Console.WriteLine("Stopping scanner");
            main.Stop();
            Console.WriteLine("Stopped scanner - exit");
        }
    }
}
