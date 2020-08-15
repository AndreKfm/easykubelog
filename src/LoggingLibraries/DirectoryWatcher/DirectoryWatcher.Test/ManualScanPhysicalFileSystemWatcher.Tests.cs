using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace DirectoryWatcher.Tests
{
    public class ManualScanPhysicalFileSystemWatcherFileListTests
    {
        [Fact]
        void CreateObject()
        {
            ManualScanPhysicalFileSystemWatcherFileList manual = new ManualScanPhysicalFileSystemWatcherFileList();
        }


        [Fact]
        void AddFile()
        {
            ManualScanPhysicalFileSystemWatcherFileList manual = new ManualScanPhysicalFileSystemWatcherFileList();

            Assert.True(manual.AddFileTruncPath("c:\\file.txt"));
            Assert.False(manual.AddFileTruncPath("c:\\file.txt"));
            Assert.False(manual.AddFileTruncPath("/file.txt"));
            Assert.False(manual.AddFileTruncPath("\\file.txt"));
            Assert.False(manual.AddFileTruncPath("file.txt"));
            Assert.False(manual.AddFileTruncPath("c:\\otherPath\\file.txt"));
            Assert.False(manual.AddFileTruncPath("c:\\andAnother\\otherPath\\file.txt"));
            Assert.True(manual.AddFileTruncPath("c:\\andAnother\\otherPath\\file2.txt"));
        }

        [Fact]
        void RemoveFile()
        {
            ManualScanPhysicalFileSystemWatcherFileList manual = new ManualScanPhysicalFileSystemWatcherFileList();

            Assert.True(manual.AddFileTruncPath("c:\\file.txt"));
            Assert.False(manual.AddFileTruncPath("c:\\file.txt"));
            Assert.False(manual.AddFileTruncPath("c:\\otherPath\\file.txt"));
            Assert.False(manual.AddFileTruncPath("c:\\andAnother\\otherPath\\file.txt"));
            Assert.True(manual.AddFileTruncPath("c:\\andAnother\\otherPath\\file2.txt"));

            Assert.True(manual.RemoveFileIgnorePath("file.txt"));
            Assert.False(manual.RemoveFileIgnorePath("file.txt"));
            Assert.True(manual.AddFileTruncPath("c:\\file.txt"));
            Assert.True(manual.RemoveFileIgnorePath("\\file.txt"));
            Assert.False(manual.RemoveFileIgnorePath("\\file.txt"));
        }

        [Fact]
        void NewFileDateTime()
        {
            DateTime time = DateTime.Now;
            ManualScanPhysicalFileSystemWatcherFileList manual = new ManualScanPhysicalFileSystemWatcherFileList();
            Assert.True(manual.SetOrAddFileInfo("c:\\file.txt", (time, 0)));
            Assert.False(manual.AddFileTruncPath("c:\\file.txt"));
            Assert.True(manual.RemoveFileIgnorePath("c:\\file.txt"));
            Assert.True(manual.AddFileTruncPath("c:\\file.txt"));
            Assert.True(manual.SetOrAddFileInfo("c:\\file.txt", (time, 0)));
            Assert.True(manual.SetOrAddFileInfo("c:\\file.txt", (time + TimeSpan.FromDays(2), 0)));
            Assert.True(manual.SetOrAddFileInfo("c:\\file.txt", default));
            Assert.True(manual.SetOrAddFileInfo("c:\\file.txt", (DateTime.MaxValue, 0)));
            Assert.True(manual.SetOrAddFileInfo("c:\\file.txt", (DateTime.MinValue, 0)));
        }

        [Fact]
        void TestOSFileNameNormalization()
        {
            ManualScanPhysicalFileSystemWatcherFileList manual = new ManualScanPhysicalFileSystemWatcherFileList();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.True(manual.AddFileTruncPath("c:\\file.txt"));
                Assert.False(manual.AddFileTruncPath("c:\\file.txt"));
                Assert.False(manual.AddFileTruncPath("c:\\File.txt")); // Casing will be ignored under windows - under Linux this test result is true
            }
        }

        [Fact]
        void GetFileList()
        {
            var time = DateTime.Now;
            ManualScanPhysicalFileSystemWatcherFileList manual = new ManualScanPhysicalFileSystemWatcherFileList();
            Dictionary<string, (DateTime lastWriteUtc, long fileLength)> list = manual.GetFileListCopy();
            Assert.True(list.Count == 0);
            Assert.True(manual.SetOrAddFileInfo("c:\\file.txt", (time, 0)));

            list = manual.GetFileListCopy();
            Assert.True(list.Count == 1);

            Assert.True(manual.SetOrAddFileInfo("c:\\file2.txt", (time + TimeSpan.FromSeconds(1), 0))); list = manual.GetFileListCopy();
            Assert.False(list.Count == 1);
            Assert.False(list.Count == 3);
            Assert.True(manual.RemoveFileIgnorePath("file.txt")); list = manual.GetFileListCopy();
            Assert.False(list.Count == 2);
            Assert.False(manual.RemoveFileIgnorePath("file.txt")); list = manual.GetFileListCopy();
            Assert.False(list.Count == 2);
            Assert.True(list.Count == 1);
            Assert.True(manual.RemoveFileIgnorePath("/gzud/shsuHSHS/file2.txt")); list = manual.GetFileListCopy();
            Assert.True(list.Count == 0);

        }
    }



}

