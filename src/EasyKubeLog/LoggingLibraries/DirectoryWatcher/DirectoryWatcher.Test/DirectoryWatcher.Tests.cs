using Moq;
using Xunit;

namespace DirectoryWatcher.Tests
{
    public class FileDirectoryWatcherTests
    {
        FileDirectoryWatcherSettings _settings = new FileDirectoryWatcherSettings("/test.txt");

        [Fact]
        public void CreateFileDirectoryWatcher()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            var settings = new FileDirectoryWatcherSettings();
            _ = new FileDirectoryWatcher(settings);
            _ = new FileDirectoryWatcher(settings, null);
            _ = new FileDirectoryWatcher(settings, watcher.Object); ;
        }

        [Fact]
        public void Open_With_ValidPath()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            FileDirectoryWatcher w = new FileDirectoryWatcher(_settings, watcher.Object);
            w.Open();
        }

        [Fact]
        public void Open_With_ValidPathAndFilter()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            FileDirectoryWatcher w = new FileDirectoryWatcher(_settings, watcher.Object);
            w.Open(new FilterAndCallbackArgument("*.log"));
        }

        [Fact]
        public void Open_Dispose()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            FileDirectoryWatcher w = new FileDirectoryWatcher(_settings, watcher.Object);
            w.Open();
            w.Dispose();
        }

        [Fact]
        public void Second_Open_AfterDispose()
        {
            Mock<IFileSystemWatcher> watcher = new Mock<IFileSystemWatcher>();
            FileDirectoryWatcher w = new FileDirectoryWatcher(_settings, watcher.Object);
            w.Open();
            w.Dispose();
            Assert.Throws<System.NullReferenceException>(() => w.Open());
        }

    }
}
