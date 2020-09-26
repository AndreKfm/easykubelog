using DirectoryWatcher;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
// ReSharper disable All

namespace ManualFileSystemWatcherConsole
{

    public class SimpleTestCreateNewFile
    {
        public SimpleTestCreateNewFile()
        { }

        string DeleteAndReturnTempDirectoryName()
        {
            var temp = Path.GetTempPath();
            var scanDirectory = Path.Combine(temp, "_DELETE_ManualScanPhysicalFileSystemTests");
            if (Directory.Exists(scanDirectory))
            {
                Directory.Delete(scanDirectory, true);
            }
            return scanDirectory;
        }
        string GetAndPrepareTempDirectory()
        {
            var temp = Path.GetTempPath();
            var scanDirectory = DeleteAndReturnTempDirectoryName();

            Directory.CreateDirectory(scanDirectory);
            return scanDirectory;
        }

        string TempFileName(string tempDir, string fileName)
        {
            return Path.Combine(tempDir, fileName);
        }

        public void TestNewFiles()
        {
            var scanDirectory = GetAndPrepareTempDirectory();
            var m = new ManualScanPhysicalFileSystemWatcher(new ManualScanPhysicalFileSystemWatcherSettings { ScanDirectory = scanDirectory, ScanSpeedInSeconds = 1 });

            ManualResetEvent changeDetected = new ManualResetEvent(false);
            ManualResetEvent scanInitialized = new ManualResetEvent(false);
            m.Open(new FilterAndCallbackArgument(String.Empty, (o, args) =>
            {
                //if (args.ChangeType == IFileSystemWatcherChangeType.Created)
                changeDetected.Set();
            },
            o =>
            {
                scanInitialized.Set();
            }));

            var completedScan = scanInitialized.WaitOne(5000);
            Assert.True(completedScan);

            var file = File.Create(TempFileName(scanDirectory, "NewFile1.txt"));
            var completed = changeDetected.WaitOne(5000);
            Assert.True(completed);
            file.Dispose();

            m.Dispose();

            DeleteAndReturnTempDirectoryName();

        }
    }

    class Program
    {

        static void CallbackChanges(object o, WatcherCallbackArgs args)
        {
            Console.WriteLine($"[{args.FileName}] - [{args.ChangeType}]");
        }

        static void Main(string[] args)
        {
            var consoleTracer = new ConsoleTraceListener(true);
            Trace.Listeners.Add(consoleTracer);
            consoleTracer.Name = "ManualFileSystemWatcherTrace";
            var dir = @"c:\test\manual";

            if (args.Any())
                dir = args[0];

            Console.WriteLine($"Scan directory: {dir}");
            //SimpleTestCreateNewFile s = new SimpleTestCreateNewFile(); s.TestNewFiles(); return;

            ManualScanPhysicalFileSystemWatcherSettings settings =
                new ManualScanPhysicalFileSystemWatcherSettings { ScanDirectory = dir, ScanSpeedInSeconds = 1 };
            ManualScanPhysicalFileSystemWatcher w = new ManualScanPhysicalFileSystemWatcher(settings);
            w.Open(new FilterAndCallbackArgument(String.Empty, CallbackChanges));

            // Uncomment for Physical file watcher - for testing purposes only
            //PhysicalFileSystemWatcherWrapperSettings settingsP =
            //    new PhysicalFileSystemWatcherWrapperSettings { ScanDirectory = dir };
            //PhysicalFileSystemWatcherWrapper wP = new PhysicalFileSystemWatcherWrapper(settingsP);
            //wP.Open(new FilterAndCallbackArgument(String.Empty, CallbackChanges));


            Console.WriteLine("Waiting for file changes\r\n\r\n");
            Console.ReadLine();
        }
    }
}
