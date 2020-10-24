using System;
using System.Collections.ObjectModel;
using System.IO;
using Scanner.Domain;
using Scanner.Domain.Ports;
using Scanner.Domain.Shared;
using Scanner.Infrastructure.Adapter.LogDirWatcher;
using Scanner.Infrastructure.Adapter.LogDirWatcher.ManualDirectoryScan;
using SharedKernel;
using Xunit;


namespace Scanner.Infrastructure.Test
{
    public class ScannerExecutorWatcherInfrastructureTest
    {

        private string PrepareAndGetDirectory
        {
            get
            {
                var dir = @"c:\temp\polldir";
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        internal class TestScannerEventLister : IEventListener
        {
            public void NewEvent(Event newEvent)
            {
                Console.WriteLine($"New event: {newEvent.Name}");
            }
        }


        private (ILogDirWatcher watcher, string dir) CreateWatcherAndDir()
        {
            string dir = PrepareAndGetDirectory;
            IManualScanDirectory manualScanDirectory = new ManualScanDirectory();

            ManualDirectoryScanAndGenerateDifferenceToLastScan pollDirectoryForChanges =
                new ManualDirectoryScanAndGenerateDifferenceToLastScan(new ManualDirectoryScanAndGenerateDifferenceToLastScanSettings(dir),
                    manualScanDirectory);

            ILogDirWatcher watcher = new LogDirectoryWatcher(pollDirectoryForChanges);
            return (watcher, dir);
        }


        [Fact]
        public void Create()
        {
            TestScannerEventLister listener = new TestScannerEventLister();
            var watcher = CreateWatcherAndDir().watcher;
            ScannerWatcherExecutor executor = new ScannerWatcherExecutor(listener, watcher);
        }


        private ReadOnlyCollection<FileEntry> GetChangeFiles(ILogDirWatcher watcher)
        {
            watcher.ScanDirectory();
            return watcher.GetChangedFiles();
        }


        [Fact]
        public void CheckForChanges()
        {
            TestScannerEventLister listener = new TestScannerEventLister();
            var (watcher, dir)= CreateWatcherAndDir();
            ScannerWatcherExecutor executor = new ScannerWatcherExecutor(listener, watcher);

            var files = GetChangeFiles(watcher);
            Assert.True(files.Count == 0);

            var fileName = Path.Combine(dir, "dummy.txt");
            var file = File.Create(fileName);
            file.Close();

            files = GetChangeFiles(watcher);
            Assert.True(files.Count > 0);
            Assert.True(files[0].ChangeType == FileSystemWatcherChangeType.Created);

            files = GetChangeFiles(watcher);
            Assert.True(files.Count == 0);

            file = File.OpenWrite(fileName);
            file.WriteByte(1);
            file.Close();
            files = GetChangeFiles(watcher);
            Assert.True(files.Count > 0);
            Assert.True(files[0].ChangeType == FileSystemWatcherChangeType.Changed);

            File.Delete(fileName);
            files = GetChangeFiles(watcher);
            Assert.True(files.Count > 0);
            Assert.True(files[0].ChangeType == FileSystemWatcherChangeType.Deleted);

        }
    }
}
