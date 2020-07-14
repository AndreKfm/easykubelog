using DirectoryWatching;
using Moq;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Xml.Serialization;
using Xunit;

namespace FileListClasses.Test
{
    using FileListType = ReadOnlyCollection<FileEntry>;

    public class FileListUnitTests
    {
        [Fact]
        public void FileList_CreateObject()
        {
            _ = new FileList(directoryToWatch);
        }

        //const string directoryToWatch = @"C:\test\deleteme\xwatchertest";
        const string directoryToWatch = @"IGNORE_FOR_TESTS";



        [Fact]
        public void FileList_StartWatching()
        {
            var callback = new Action<FileListType>((FileListType list) => { });
            FileList w = new FileList(directoryToWatch);
            w.Start(directoryToWatch, _ => callback(_));
        }

        [Fact]
        public void FileList_StartWatching_ThenDispose()
        {
            var callback = new Action<FileListType>((FileListType list) => {});
            FileList w = new FileList(directoryToWatch);
            w.Start(directoryToWatch, _ => callback(_));
            w.Dispose();
        }


        [Fact]
        public void FileList_StartWatching_ChangeFilesCheckState()
        {
            FilterAndCallbackArgument local = null;
            var a = new Action<string, FilterAndCallbackArgument>((string filter, FilterAndCallbackArgument arg) =>
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
            m.Setup(x => x.Open(It.IsAny<String>(), It.IsAny<FilterAndCallbackArgument>())).Callback<string , FilterAndCallbackArgument>((string x, FilterAndCallbackArgument arg) => a(x, arg)).Returns(true);
            FileList w = new FileList(directoryToWatch, m.Object, 0 /** NO throttling !!! */);
            w.Start(directoryToWatch, _ => lc(_));

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
            local.action(this, new WatcherCallbackArgs(fileName, ft));
            Assert.True(lastEntry.FileName == fileName && lastEntry.LastChanges == ft);
        }
    }
}
