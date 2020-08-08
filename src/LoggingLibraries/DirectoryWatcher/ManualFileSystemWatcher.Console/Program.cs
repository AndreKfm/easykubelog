using System;
using System.Runtime.CompilerServices;
using DirectoryWatcher;

namespace ManualFileSystemWatcherConsole
{
    class Program
    {

        static void CallbackChanges(object o, WatcherCallbackArgs args)
        {
            Console.WriteLine($"[{args.FileName}] - [{args.ChangeType}]");
        }

        static void Main(string[] args)
        {
            ManualScanPhysicalFileSystemWatcherSettings settings = 
                new ManualScanPhysicalFileSystemWatcherSettings { ScanDirectory = @"c:\test\manual", ScanSpeedInSeconds = 1 };
            ManualScanPhysicalFileSystemWatcher w = new ManualScanPhysicalFileSystemWatcher(settings);
            w.Open(new FilterAndCallbackArgument(String.Empty, CallbackChanges));
            Console.WriteLine("Waiting for file changes\r\n\r\n");
            Console.ReadLine();
        }
    }
}
