using System;
using System.Collections.Generic;
using System.Text;

namespace WatchedFileList.Test
{
    using Moq;
    using Xunit;
    public class CurrentFileListTests
    {
        [Fact]
        public void AddFile()
        {
            Mock<IFileReadonlyWrapper> m = new Mock<IFileReadonlyWrapper>();
            CurrentFileList c = new CurrentFileList();
            c.AddFile(new CurrentFileEntry("test1.txt", m.Object));
            var l = c.GetList();
            Assert.True(l.Count == 1);
            Assert.True(l.ContainsKey("test1.txt"));
        }

        [Fact]
        public void RemoveFile()
        {
            Mock<IFileReadonlyWrapper> m = new Mock<IFileReadonlyWrapper>();
            CurrentFileList c = new CurrentFileList();
            Assert.False(c.RemoveFile("test1.txt"));
            Assert.True(c.AddFile(new CurrentFileEntry("test1.txt", m.Object)));
            var l = c.GetList();
            Assert.True(l.Count == 1);
            Assert.True(l.ContainsKey("test1.txt"));
            Assert.True(c.RemoveFile("test1.txt"));
            Assert.True(c.GetList().Count == 0);
        }
    }
}
