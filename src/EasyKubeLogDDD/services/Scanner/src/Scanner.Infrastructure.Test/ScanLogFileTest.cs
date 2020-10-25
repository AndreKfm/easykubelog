using System.Threading;
using Scanner.Domain.Shared;
using Xunit;
using Moq;
using Scanner.Infrastructure.Adapter.ScanLogFiles;

namespace Scanner.Infrastructure.Test
{
    using System;

    public class AutoCurrentFileListTests
    {
        [Fact]
        public void SimpleCreate()
        {

            AutoCurrentFileList autoCurrentFileList = new AutoCurrentFileList();
            Assert.True(autoCurrentFileList != null);
        }


    }

}
