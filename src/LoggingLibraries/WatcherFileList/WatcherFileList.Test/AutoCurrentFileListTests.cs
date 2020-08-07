using System;

namespace WatcherFileListClasses.Test
{
    using DirectoryWatcher;
    using Moq;
    using System.Threading;
    using Xunit;
    public class AutoCurrentFileListTests
    {
        [Fact]
        public void SimpleCreate()
        {
            AutoCurrentFileList autoCurrentFileList = new AutoCurrentFileList();
            Assert.True(autoCurrentFileList != null);
        }

        [Fact]
        public void SimpleCreate_WithMockedParameter_IGetFile()
        {
            var m = new Mock<IGetFile>();           
            AutoCurrentFileList autoCurrentFileList = new AutoCurrentFileList(m.Object);
            Assert.True(autoCurrentFileList != null);
        }

        public class MockFileWrapper : IFile
        {
            
            public string ReadLineFromCurrentPositionToEnd(long maxStringSize = 16384)
            {
                return CurrentOutput;
            }
            public string CurrentOutput { get; set; } = String.Empty;
        }

        [Fact]
        public void SimulateFileWriting_CheckResult()
        {
            var m = new Mock<IGetFile>();
            var wrapper = new MockFileWrapper();
            m.Setup((x) => x.GetFile(It.IsAny<string>())).Returns(wrapper);
            AutoCurrentFileList autoCurrentFileList = new AutoCurrentFileList(m.Object);


            Action<object, WatcherCallbackArgs> actionFileChanges = null;

            void FilterCallback(string filter, FilterAndCallbackArgument callback)
            {
                actionFileChanges = callback.action;
            }

            var mwatcher = new Mock<IFileSystemWatcher>();
            mwatcher.Setup((x) => x.Open(It.IsAny<string>(), It.IsAny<FilterAndCallbackArgument>())).Callback((Action<string, FilterAndCallbackArgument>)FilterCallback).Returns(true);

            autoCurrentFileList.Start("dummy", mwatcher.Object);
            string mustBeThis = "must be this";
            string lastOutput = String.Empty;
            string lastFileName = String.Empty;
            AutoResetEvent waitForInput = new AutoResetEvent(false);
            var task = autoCurrentFileList.BlockingReadAsyncNewOutput((output, token) =>
            {
                lastOutput = output.Lines;
                lastFileName = output.Filename;
                waitForInput.Set();
            });
            wrapper.CurrentOutput = mustBeThis;
            actionFileChanges(null, new WatcherCallbackArgs("file1.txt", IFileSystemWatcherChangeType.Changed));
            Assert.True(waitForInput.WaitOne(100));
            Assert.True(lastOutput == mustBeThis);
            Assert.True(lastFileName == "file1.txt");

            string mustBeThis2 = "### !CHANGED! öäüÖÄÜ ###";
            wrapper.CurrentOutput = mustBeThis2;
            actionFileChanges(null, new WatcherCallbackArgs("file2.txt", IFileSystemWatcherChangeType.Changed));
            Assert.True(waitForInput.WaitOne(100));
            Assert.True(lastOutput == mustBeThis2);
            Assert.True(lastFileName == "file2.txt");


        }
    }
}
