using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DirectoryWatcher;
using Xunit;

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
            m.Open(new FilterAndCallbackArgument(String.Empty, (object o, WatcherCallbackArgs args) =>
            {
                //if (args.ChangeType == IFileSystemWatcherChangeType.Created)
                changeDetected.Set();
            },
            (object o) =>
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

            SimpleTestCreateNewFile s = new SimpleTestCreateNewFile(); s.TestNewFiles();
            return;

            ManualScanPhysicalFileSystemWatcherSettings settings = 
                new ManualScanPhysicalFileSystemWatcherSettings { ScanDirectory = @"c:\test\manual", ScanSpeedInSeconds = 1 };
            ManualScanPhysicalFileSystemWatcher w = new ManualScanPhysicalFileSystemWatcher(settings);
            w.Open(new FilterAndCallbackArgument(String.Empty, CallbackChanges));
            Console.WriteLine("Waiting for file changes\r\n\r\n");
            Console.ReadLine();
        }
    }
}
