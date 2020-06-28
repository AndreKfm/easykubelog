using Moq;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace DirectoryWatcher.Test
{
    public class DirectoryWatcherTests
    {
        
        [Fact]
        public void CreateDirectoryWatcher()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            
            DirectoryWatcher w1 = new DirectoryWatcher();
            DirectoryWatcher w2 = new DirectoryWatcher(null);
            DirectoryWatcher w3 = new DirectoryWatcher(watcher.Object); ;
        }

        [Fact]
        public void Open_With_ValidPath()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            DirectoryWatcher w = new DirectoryWatcher(watcher.Object);
            w.Open("/test.txt");
        }

        [Fact]
        public void Open_With_ValidPathAndFilter()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            DirectoryWatcher w = new DirectoryWatcher(watcher.Object);
            w.Open("/test.txt", "*.log");
        }

        [Fact]
        public void Second_Open_AfterDispose()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            DirectoryWatcher w = new DirectoryWatcher(watcher.Object);
            w.Open("/test.txt");
            w.Dispose();
            Assert.Throws<System.NullReferenceException>(() => w.Open("/test2.txt"));            
        }

    }
}
