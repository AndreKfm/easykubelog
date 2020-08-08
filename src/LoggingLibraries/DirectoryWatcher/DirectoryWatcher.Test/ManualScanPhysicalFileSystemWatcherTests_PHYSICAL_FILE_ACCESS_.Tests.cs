using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Xunit;

namespace DirectoryWatcher.Tests
{
    public class ManualScanPhysicalFileSystemWatcherTests_PHYSICAL_FILE_ACCESS_
    {
        [Fact]
        void CreateManualScanPhysicalFileSystemWatcherObject()
        {
            var m1 = new ManualScanPhysicalFileSystemWatcher();
            var m2 = new ManualScanPhysicalFileSystemWatcher(new ManualScanPhysicalFileSystemWatcherSettings { ScanDirectory = "c:\\temp" });
        }

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



        void CreateFileHelper(Func<string, FileStream> createFileBefore, Func<string, FileStream> createFileAfter, IFileSystemWatcherChangeType type)
        {
            var scanDirectory = GetAndPrepareTempDirectory();

            FileStream fileBefore = createFileBefore == null ? null : createFileBefore(scanDirectory);


            var m = new ManualScanPhysicalFileSystemWatcher(new ManualScanPhysicalFileSystemWatcherSettings { ScanDirectory = scanDirectory, ScanSpeedInSeconds = 0 });

            ManualResetEvent changeDetected = new ManualResetEvent(false);
            ManualResetEvent scanInitialized = new ManualResetEvent(false);
            m.Open(new FilterAndCallbackArgument(String.Empty, (object o, WatcherCallbackArgs args) =>
            {
                if (args.ChangeType == type)
                    changeDetected.Set();
            },
            (object o) =>
            {
                scanInitialized.Set();
            }));

            var completedScan = scanInitialized.WaitOne(500);
            Assert.True(completedScan);


            FileStream file = createFileAfter == null ? null : createFileAfter(scanDirectory);
            var completed = changeDetected.WaitOne(500);
            Assert.True(completed);
            file?.Dispose();
            fileBefore?.Dispose();

            m.Dispose();

            DeleteAndReturnTempDirectoryName();
        }

        [Fact]
        void TestNewFiles()
        {
            CreateFileHelper(null, (scanDirectory) => File.Create(TempFileName(scanDirectory, "NewFile1.txt")), IFileSystemWatcherChangeType.Created); ;
        }
        FileStream OpenFile(string name)
        {
            return File.Open(name, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        }

        [Fact]
        void TestChangedFiles()
        {

            CreateFileHelper(
                (scanDirectory) => OpenFile(TempFileName(scanDirectory, "NewFile1.txt")),
                (scanDirectory) => 
                { 
                    var file = OpenFile(TempFileName(scanDirectory, "NewFile1.txt")); 
                    file.WriteByte(65); 
                    file.Close(); 
                    return null; 
                }, IFileSystemWatcherChangeType.Changed); ;
        }

        [Fact]
        void TestDeleteFiles()
        {
            CreateFileHelper(
                (scanDirectory) => OpenFile(TempFileName(scanDirectory, "NewFile1.txt")),
                (scanDirectory) => { File.Delete(TempFileName(scanDirectory, "NewFile1.txt")); return null; }, IFileSystemWatcherChangeType.Deleted); ;
        }
    }
}
