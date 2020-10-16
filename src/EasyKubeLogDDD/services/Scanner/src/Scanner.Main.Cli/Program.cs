using System;
using Scanner.Domain;
using Scanner.Domain.Ports;
using Scanner.Infrastructure.Adapter;

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

            LogFileChangedHandler handler = new LogFileChangedHandler();
            LogDirWatcher watcher = new LogDirWatcher();
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
