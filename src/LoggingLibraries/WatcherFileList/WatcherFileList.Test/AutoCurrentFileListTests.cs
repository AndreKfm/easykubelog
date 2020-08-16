using System;

namespace WatcherFileListClasses.Test
{
    using DirectoryWatcher;
    using FileToolsClasses;
    using Microsoft.Extensions.Options;
    using Moq;
    using System.IO;
    using System.Threading;
    using Xunit;
    public class AutoCurrentFileListTests
    {
        [Fact]
        public void SimpleCreate()
        {
            AutoCurrentFileList autoCurrentFileList = new AutoCurrentFileList(_settingsFileWatcher, _settingsAutoCurrentFileList);
            Assert.True(autoCurrentFileList != null);
        }

        [Fact]
        public void SimpleCreate_WithMockedParameter_IGetFile()
        {
            var m = new Mock<IGetFile>();           
            AutoCurrentFileList autoCurrentFileList = new AutoCurrentFileList(_settingsFileWatcher, _settingsAutoCurrentFileList, m.Object);
            Assert.True(autoCurrentFileList != null);
        }

        public class MockFileWrapper : IFile
        {
            
            public (string, ReadLine) ReadLineFromCurrentPositionToEnd(long maxStringSize = 16384)
            {
                return (CurrentOutput, ReadLine.BufferSufficient);
            }
            public string CurrentOutput { get; set; } = String.Empty;
        }

        IOptions<AutoCurrentFileListSettings> _settingsAutoCurrentFileList = Options.Create(new AutoCurrentFileListSettings { FilterDirectoriesForwardFilesOnly = false });
        IOptions<FileDirectoryWatcherSettings> _settingsFileWatcher = Options.Create(new FileDirectoryWatcherSettings { ScanDirectory = Path.GetTempPath(), UseManualScan = false });


        [Fact]
        public void SimulateFileWriting_CheckResult()
        {
            var m = new Mock<IGetFile>();
            var wrapper = new MockFileWrapper();
            m.Setup((x) => x.GetFile(It.IsAny<string>())).Returns(wrapper);
            
            AutoCurrentFileList autoCurrentFileList = new AutoCurrentFileList(_settingsFileWatcher, _settingsAutoCurrentFileList, m.Object);


            Action<object, WatcherCallbackArgs> actionFileChanges = null;
            Action<object> actionScanning = null;

            void FilterCallback(FilterAndCallbackArgument callback)
            {
                actionFileChanges = callback.ActionChanges;
                actionScanning = callback.ActionScanning;
            }

            var mwatcher = new Mock<IFileSystemWatcher>();
            mwatcher.Setup((x) => x.Open(It.IsAny<FilterAndCallbackArgument>())).Callback((Action<FilterAndCallbackArgument>)FilterCallback).Returns(true);

            autoCurrentFileList.Start(mwatcher.Object);
            string mustBeThis = "must be this";
            string lastOutput = String.Empty;
            string lastFileName = String.Empty;
            AutoResetEvent waitForInput = new AutoResetEvent(false);
            var task = autoCurrentFileList.BlockingReadAsyncNewOutput((output, token) =>
            {
                lastOutput = output.Lines;
                lastFileName = output.FileName;
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
