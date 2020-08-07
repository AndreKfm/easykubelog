using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
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

            Assert.True (manual.AddFileTruncPath("c:\\file.txt"));
            Assert.False(manual.AddFileTruncPath("c:\\file.txt"));
            Assert.False(manual.AddFileTruncPath("/file.txt"));
            Assert.False(manual.AddFileTruncPath("\\file.txt"));
            Assert.False(manual.AddFileTruncPath("file.txt"));
            Assert.False(manual.AddFileTruncPath("c:\\otherPath\\file.txt"));
            Assert.False(manual.AddFileTruncPath("c:\\andAnother\\otherPath\\file.txt"));
            Assert.True (manual.AddFileTruncPath("c:\\andAnother\\otherPath\\file2.txt"));
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

            Assert.True (manual.RemoveFileIgnorePath("file.txt"));
            Assert.False(manual.RemoveFileIgnorePath("file.txt"));
            Assert.True(manual.AddFileTruncPath("c:\\file.txt"));
            Assert.True(manual.RemoveFileIgnorePath("\\file.txt"));
            Assert.False(manual.RemoveFileIgnorePath("\\file.txt"));
        }

        [Fact]
        void NewFileOffset()
        {
            ManualScanPhysicalFileSystemWatcherFileList manual = new ManualScanPhysicalFileSystemWatcherFileList();
            Assert.True(manual.SetOrAddFileOffset("c:\\file.txt", 1));
            Assert.False(manual.AddFileTruncPath("c:\\file.txt"));
            Assert.True(manual.RemoveFileIgnorePath("c:\\file.txt"));
            Assert.True(manual.AddFileTruncPath("c:\\file.txt"));
            Assert.False(manual.SetOrAddFileOffset("c:\\file.txt", -1));
            Assert.False(manual.SetOrAddFileOffset("c:\\file.txt", -100));
            Assert.True(manual.SetOrAddFileOffset("c:\\file.txt", 0));
            Assert.True(manual.SetOrAddFileOffset("c:\\file.txt", long.MaxValue));
            Assert.False(manual.SetOrAddFileOffset("c:\\file.txt", long.MinValue));
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
            ManualScanPhysicalFileSystemWatcherFileList manual = new ManualScanPhysicalFileSystemWatcherFileList();
            ImmutableDictionary<string, long> list = manual.GetFileList();
            Assert.True(list.Count == 0);
            Assert.True(manual.SetOrAddFileOffset("c:\\file.txt", 1));
            
            list = manual.GetFileList();
            Assert.True(list.Count == 1);

            Assert.True(manual.SetOrAddFileOffset("c:\\file2.txt", 1)); list = manual.GetFileList();
            Assert.False(list.Count == 1);
            Assert.False(list.Count == 3);
            Assert.True(manual.RemoveFileIgnorePath("file.txt")); list = manual.GetFileList();
            Assert.False(list.Count == 2);
            Assert.False(manual.RemoveFileIgnorePath("file.txt")); list = manual.GetFileList();
            Assert.False(list.Count == 2);
            Assert.True(list.Count == 1);
            Assert.True(manual.RemoveFileIgnorePath("/gzud/shsuHSHS/file2.txt")); list = manual.GetFileList();
            Assert.True(list.Count == 0);

        }

        // AddFile
        // RemoveFile
        // NewFileOffset
        // CurrentFileOffset


    }
}
