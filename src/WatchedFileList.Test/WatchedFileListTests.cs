using System;
using System.Collections.ObjectModel;
using Xunit;

namespace WatchedFileList.Test
{
    using FileListType = ReadOnlyCollection<FileEntry>;

    public class WatchedFileListUnitTests
    {
        [Fact]
        public void WatchFileList_CreateObject()
        {
            _ = new WatchFileList(directoryToWatch);
        }

        const string directoryToWatch = @"C:\test\deleteme\xwatchertest";

        void Callback(FileListType list)
        {

        }


        [Fact]
        public void WatchFileList_StartWatching()
        {
            WatchFileList w = new WatchFileList(directoryToWatch);
            w.Start(directoryToWatch, _ => Callback(_));
        }
    }
}
