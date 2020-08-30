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
        const string directoryToWatch = @"IGNORE_FOR_TESTS";



        [Fact]
        public void FileList_StartWatching()
        {
            var callback = new Action<FileListType>((FileListType list) => { });
            WatcherFileList w = new WatcherFileList(new FileDirectoryWatcherSettings());
            w.Start(_ => callback(_));
        }

        [Fact]
        public void FileList_StartWatching_ThenDispose()
        {
            var callback = new Action<FileListType>((FileListType list) => { });
            WatcherFileList w = new WatcherFileList(new FileDirectoryWatcherSettings());
            w.Start(_ => callback(_));
            w.Dispose();
        }


        [Fact]
        public void FileList_StartWatching_ChangeFilesCheckState()
        {
            FilterAndCallbackArgument local = null;
            var a = new Action<FilterAndCallbackArgument>((FilterAndCallbackArgument arg) =>
            {
                local = arg;
            });

            FileEntry lastEntry = new FileEntry();

            var lc = new Action<FileListType>((FileListType list) =>
            {
                foreach (var e in list)
                {
                    lastEntry = e;
                    Debug.WriteLine($"{e.FileName}: {e.LastChanges}");
                }
            });


            Mock<IFileSystemWatcher> m = new Mock<IFileSystemWatcher>();
            var f = new FilterAndCallbackArgument("*.txt");
            m.Setup(x => x.Open(It.IsAny<FilterAndCallbackArgument>())).Callback<FilterAndCallbackArgument>((FilterAndCallbackArgument arg) => a(arg)).Returns(true);
            WatcherFileList w = new WatcherFileList(new FileDirectoryWatcherSettings(), m.Object, 0 /** NO throttling !!! */);
            w.Start(_ => lc(_));

            CheckHelper(local, "f1.txt", ref lastEntry, IFileSystemWatcherChangeType.Created);
            CheckHelper(local, "f2.txt", ref lastEntry, IFileSystemWatcherChangeType.Changed);
            CheckHelper(local, "f3.txt", ref lastEntry, IFileSystemWatcherChangeType.Deleted);
            CheckHelper(local, "f4.txt", ref lastEntry, IFileSystemWatcherChangeType.Dispose);
            CheckHelper(local, "f5.txt", ref lastEntry, IFileSystemWatcherChangeType.Error);
            CheckHelper(local, "f6.txt", ref lastEntry, IFileSystemWatcherChangeType.Rename);
            CheckHelper(local, "f7.txt", ref lastEntry, IFileSystemWatcherChangeType.All); // Shouldn't occur in productive environments

            w.Dispose();
        }

        void CheckHelper(FilterAndCallbackArgument local, string fileName, ref FileEntry lastEntry, IFileSystemWatcherChangeType ft)
        {
            local.ActionChanges(this, new WatcherCallbackArgs(fileName, ft));
            Assert.True(lastEntry.FileName == fileName && lastEntry.LastChanges == ft);
        }
    }
}
