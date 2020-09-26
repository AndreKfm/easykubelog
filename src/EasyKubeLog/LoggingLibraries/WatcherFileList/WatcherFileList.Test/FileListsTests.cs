using DirectoryWatcher;
using Moq;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Xunit;

namespace WatcherFileListClasses.Test
{
    using FileListType = ReadOnlyCollection<FileEntry>;

    public class FileListUnitTests
    {
        [Fact]
        public void FileList_CreateObject()
        {
            _ = new WatcherFileList(new FileDirectoryWatcherSettings());
        }

        //const string directoryToWatch = @"C:\test\deleteme\xwatchertest";


        [Fact]
        public void FileList_StartWatching()
        {
            var callback = new Action<FileListType>(list => { });
            WatcherFileList w = new WatcherFileList(new FileDirectoryWatcherSettings());
            w.Start(_ => callback(_));
        }

        [Fact]
        public void FileList_StartWatching_ThenDispose()
        {
            var callback = new Action<FileListType>(list => { });
            WatcherFileList w = new WatcherFileList(new FileDirectoryWatcherSettings());
            w.Start(_ => callback(_));
            w.Dispose();
        }


        [Fact]
        public void FileList_StartWatching_ChangeFilesCheckState()
        {
            FilterAndCallbackArgument local = null;
            var a = new Action<FilterAndCallbackArgument>(arg =>
            {
                local = arg;
            });

            FileEntry lastEntry = new FileEntry();

            var lc = new Action<FileListType>(list =>
            {
                foreach (var e in list)
                {
                    lastEntry = e;
                    Debug.WriteLine($"{e.FileName}: {e.LastChanges}");
                }
            });


            Mock<IFileSystemWatcher> m = new Mock<IFileSystemWatcher>();
            m.Setup(x => x.Open(It.IsAny<FilterAndCallbackArgument>())).Callback((FilterAndCallbackArgument arg) =>
            {
                if (arg == null) throw new ArgumentNullException(nameof(arg));
                a(arg);
            }).Returns(true);
            WatcherFileList w = new WatcherFileList(new FileDirectoryWatcherSettings(), m.Object);
            w.Start(_ => lc(_));

            CheckHelper(local, "f1.txt", ref lastEntry, FileSystemWatcherChangeType.Created);
            CheckHelper(local, "f2.txt", ref lastEntry, FileSystemWatcherChangeType.Changed);
            CheckHelper(local, "f3.txt", ref lastEntry, FileSystemWatcherChangeType.Deleted);
            CheckHelper(local, "f4.txt", ref lastEntry, FileSystemWatcherChangeType.Dispose);
            CheckHelper(local, "f5.txt", ref lastEntry, FileSystemWatcherChangeType.Error);
            CheckHelper(local, "f6.txt", ref lastEntry, FileSystemWatcherChangeType.Rename);
            CheckHelper(local, "f7.txt", ref lastEntry, FileSystemWatcherChangeType.All); // Shouldn't occur in productive environments

            w.Dispose();
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        void CheckHelper(FilterAndCallbackArgument local, string fileName, ref FileEntry lastEntry, FileSystemWatcherChangeType ft)
        {
            local.ActionChanges(this, new WatcherCallbackArgs(fileName, ft));
            Assert.True(lastEntry?.FileName == fileName && lastEntry?.LastChanges == ft);
        }
    }
}
