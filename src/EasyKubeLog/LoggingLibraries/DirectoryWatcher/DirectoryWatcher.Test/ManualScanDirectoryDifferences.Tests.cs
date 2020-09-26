using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;

namespace DirectoryWatcher.Tests
{
    using FileList = Dictionary<string, long>;

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public class ManualScanDirectoryDifferencesTests
    {
        [Fact]
        void CreateManualScanDirectoryDifferences()
        {
            // ReSharper disable once UnusedVariable
            var m = new ManualScanDirectoryDifferences();
        }

        FileList BuildFileList(IEnumerable<string> files, long len = 0)
        {
            FileList list = new FileList();
            foreach (var file in files)
            {
                list.Add(file, len);
            }
            return list;
        }

        [Fact]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        void CheckForNewFiles()
        {
            var m = new ManualScanDirectoryDifferences();

            FileList oldList = BuildFileList(new[] { "a", "b", "c" });
            FileList newList1 = BuildFileList(new[] { "a", "b", "c", "d" });
            FileList newList2 = BuildFileList(new[] { "a", "b" });

            var newFiles = m.GetNewFiles(oldList, newList1);
            Assert.True(newFiles.Count() == 1);
            Assert.True(newFiles.Count(s => s.Key == "d") == 1);
            Assert.True(newFiles.Count(s => s.Key == "a") == 0);
            Assert.True(newFiles.Count(s => s.Key == "b") == 0);
            Assert.True(newFiles.Count(s => s.Key == "c") == 0);

            newList1["a"] = 2;
            newList1["b"] = 3;
            newList1["c"] = 4;
            Assert.True(newFiles.Count(s => s.Key == "d") == 1);
            Assert.True(newFiles.Count(s => s.Key == "a") == 0);
            Assert.True(newFiles.Count(s => s.Key == "b") == 0);
            Assert.True(newFiles.Count(s => s.Key == "c") == 0);

            newFiles = m.GetNewFiles(oldList, newList2);
            Assert.True(!newFiles.Any());
            Assert.False(newFiles.Count(s => s.Key == "d") == 1);
            Assert.True(newFiles.Count(s => s.Key == "a") == 0);
            Assert.True(newFiles.Count(s => s.Key == "b") == 0);
            Assert.True(newFiles.Count(s => s.Key == "c") == 0);
        }

        [Fact]
        void CheckForDeletedFiles()
        {
            var m = new ManualScanDirectoryDifferences();

            FileList oldList = BuildFileList(new[] { "a", "b", "c" });
            FileList newList1 = BuildFileList(new[] { "a", "b", "c", "d" });
            FileList newList2 = BuildFileList(new[] { "a", "b" });

            var delFiles = m.GetDeletedFiles(oldList, newList1);
            Assert.True(!delFiles.Any());
            Assert.True(delFiles.Count(s => s.Key == "d") == 0);
            Assert.True(delFiles.Count(s => s.Key == "a") == 0);
            Assert.True(delFiles.Count(s => s.Key == "b") == 0);
            Assert.True(delFiles.Count(s => s.Key == "c") == 0);

            delFiles = m.GetDeletedFiles(oldList, newList2);
            Assert.True(delFiles.Count() == 1);
            Assert.True(delFiles.Count(s => s.Key == "c") == 1);
            Assert.True(delFiles.Count(s => s.Key == "d") == 0);
            Assert.True(delFiles.Count(s => s.Key == "a") == 0);
            Assert.True(delFiles.Count(s => s.Key == "b") == 0);

            newList2["a"] = 2;
            newList2["b"] = 3;
            delFiles = m.GetDeletedFiles(oldList, newList2);
            Assert.True(delFiles.Count() == 1);
            Assert.True(delFiles.Count(s => s.Key == "c") == 1);
            Assert.True(delFiles.Count(s => s.Key == "d") == 0);
            Assert.True(delFiles.Count(s => s.Key == "a") == 0);
            Assert.True(delFiles.Count(s => s.Key == "b") == 0);
        }

        [Fact]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        void CheckForChangedFiles()
        {
            var m = new ManualScanDirectoryDifferences();

            FileList oldList = BuildFileList(new[] { "a", "b", "c" });
            FileList newList1 = BuildFileList(new[] { "a", "b", "c", "d" });

            var changed = m.GetChangedFiles(oldList, newList1);
            Assert.True(!changed.Any());
            Assert.True(changed.Count(s => s.Key == "d") == 0);
            Assert.True(changed.Count(s => s.Key == "a") == 0);
            Assert.True(changed.Count(s => s.Key == "b") == 0);
            Assert.True(changed.Count(s => s.Key == "c") == 0);


            newList1["a"] = 2;
            newList1["b"] = 3;
            changed = m.GetChangedFiles(oldList, newList1);
            Assert.True(changed.Count() == 2);
            Assert.True(changed.Count(s => s.Key == "a") == 1);
            Assert.True(changed.Count(s => s.Key == "b") == 1);
            Assert.True(changed.Count(s => s.Key == "c") == 0);
            Assert.True(changed.Where(s => s.Key == "a").Count(s => s.Value == 2) == 1);
            Assert.True(changed.Where(s => s.Key == "a").All(s => s.Value != 1));
            //Assert.True(changed.Where(s => s.Key == "a").Where(s => s.Value.lastWriteUtc == time).Count() == 1);
            //Assert.True(changed.Where(s => s.Key == "b").Where(s => s.Value.lastWriteUtc == default).Count() == 1);
            Assert.True(changed.Where(s => s.Key == "b").Count(s => s.Value == 3) == 1);

        }
    }
}
