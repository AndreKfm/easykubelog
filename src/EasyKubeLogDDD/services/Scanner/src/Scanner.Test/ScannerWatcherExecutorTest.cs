using System.Collections.Generic;
using System.Collections.ObjectModel;
using Moq;
using Scanner.Domain;
using Scanner.Domain.Ports;
using Scanner.Domain.Shared;
using Xunit;

namespace Scanner.Domain.Test
{
    public class ScannerWatcherExecutorTest
    {
        [Fact]
        public void Create()
        {
            Mock<IEventListener> listener = new Mock<IEventListener>();
            Mock<ILogDirWatcher> watcher = new Mock<ILogDirWatcher>();
            ScannerWatcherExecutor executor = new ScannerWatcherExecutor(listener.Object, watcher.Object);
        }

        [Fact]
        public void StartStop()
        {
            Mock<IEventListener> listener = new Mock<IEventListener>();
            Mock<ILogDirWatcher> watcher = new Mock<ILogDirWatcher>();
            ScannerWatcherExecutor executor = new ScannerWatcherExecutor(listener.Object, watcher.Object);
            executor.Start();
            executor.Stop();
        }

        [Fact]
        public void CheckChange()
        {
            Mock<IEventListener> listener = new Mock<IEventListener>();
            Mock<ILogDirWatcher> watcherMock = new Mock<ILogDirWatcher>();

            var collection = new List<FileEntry>(new FileEntry[] {new FileEntry { FileName = "dummy", ChangeType = FileSystemWatcherChangeType.Changed } });

            watcherMock.Setup(x => x.GetChangedFiles()).Returns(collection.AsReadOnly);

            var watcher = watcherMock.Object;

            ScannerWatcherExecutor executor = new ScannerWatcherExecutor(listener.Object, watcher);
            executor.Start();

            var changes = watcher.GetChangedFiles();

            Assert.True(changes.Count == 1);
            Assert.True(changes[0].ChangeType == FileSystemWatcherChangeType.Changed);
            Assert.True(changes[0].FileName == "dummy");

            executor.Stop();
        }

        private ReadOnlyCollection<FileEntry> GetCollection()
        {
            var collection = new List<FileEntry>(new FileEntry[]
            {
                new FileEntry{FileName = "changed", ChangeType = FileSystemWatcherChangeType.Changed},
                new FileEntry{FileName = "created", ChangeType = FileSystemWatcherChangeType.Created},
                new FileEntry{FileName = "deleted", ChangeType = FileSystemWatcherChangeType.Deleted},
                new FileEntry{FileName = "error",   ChangeType = FileSystemWatcherChangeType.Error    },
                new FileEntry{FileName = "rename",  ChangeType = FileSystemWatcherChangeType.Rename  }
            });
            return collection.AsReadOnly();
        }


        [Fact]
        public void CheckAllChangeTypes()
        {
            Mock<IEventListener> listener = new Mock<IEventListener>();
            Mock<ILogDirWatcher> watcherMock = new Mock<ILogDirWatcher>();

            var listenerInterface = listener.Object;

            var collection = GetCollection();
            watcherMock.Setup(x => x.GetChangedFiles()).Returns(collection);
            var watcher = watcherMock.Object;

            ScannerWatcherExecutor executor = new ScannerWatcherExecutor(listenerInterface, watcher);
            executor.Start();

            var changes = watcher.GetChangedFiles();


            Assert.True(changes.Count == collection.Count);
            Check(changes[0], "changed", FileSystemWatcherChangeType.Changed);
            Check(changes[1], "created", FileSystemWatcherChangeType.Created);
            Check(changes[2], "deleted", FileSystemWatcherChangeType.Deleted);
            Check(changes[3], "error", FileSystemWatcherChangeType.Error);
            Check(changes[4], "rename", FileSystemWatcherChangeType.Rename);

            executor.Stop();
        }

        void Check(FileEntry entry, string name, FileSystemWatcherChangeType changeType)
        {
            Assert.Equal(entry.ChangeType, changeType);
            Assert.Equal(entry.FileName, name);
        }


    }
}
