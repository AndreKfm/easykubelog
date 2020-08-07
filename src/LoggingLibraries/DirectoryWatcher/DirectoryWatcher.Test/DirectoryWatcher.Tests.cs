using Moq;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace DirectoryWatcher.Tests
{
    public class FileDirectoryWatcherTests
    {
        
        [Fact]
        public void CreateFileDirectoryWatcher()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            _ = new FileDirectoryWatcher();
            _ = new FileDirectoryWatcher(null);
            _ = new FileDirectoryWatcher(watcher.Object); ;
        }

        [Fact]
        public void Open_With_ValidPath()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            FileDirectoryWatcher w = new FileDirectoryWatcher(watcher.Object);
            w.Open("/test.txt");
        }

        [Fact]
        public void Open_With_ValidPathAndFilter()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            FileDirectoryWatcher w = new FileDirectoryWatcher(watcher.Object);
            w.Open("/test.txt", new FilterAndCallbackArgument("*.log"));
        }

        [Fact]
        public void Open_Dispose()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            FileDirectoryWatcher w = new FileDirectoryWatcher(watcher.Object);
            w.Open("/test.txt");
            w.Dispose();
        }

        [Fact]
        public void Second_Open_AfterDispose()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            FileDirectoryWatcher w = new FileDirectoryWatcher(watcher.Object);
            w.Open("/test1.txt");
            w.Dispose();
            Assert.Throws<System.NullReferenceException>(() => w.Open("/test2.txt"));            
        }

    }
}
