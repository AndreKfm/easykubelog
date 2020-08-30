using FileToolsClasses;
using System;
using System.IO;
using Xunit;

namespace EndlessFileStreamClasses.Test
{

    internal class RandomDirectory : IDisposable
    {
        public string DirectoryPath { get; private set; }
        public RandomDirectory()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            if (Directory.Exists(DirectoryPath) == true)
                Directory.Delete(DirectoryPath, true);
            Directory.CreateDirectory(DirectoryPath);
        }


        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath) == false)
                Directory.Delete(DirectoryPath, true);
        }
    }

    class WriteEntries : IDisposable
    {
        RandomDirectory _randDir;

        public EndlessFileStream Endless { get; private set; }
        public int Count { get; private set; }
        public WriteEntries(int count, int logSizeMBytes)
        {
            Count = count;
            _randDir = new RandomDirectory();
            Endless = new EndlessFileStream(new EndlessFileStreamSettings { BaseDirectory = _randDir.DirectoryPath, MaxLogFileSizeInMByte = logSizeMBytes, MaxLogFileSizeInKByte = 1 });

            for (int l = 0; l < count; ++l)
            {
                Endless.Writer.WriteToFileStream(l.ToString()).Wait();
            }
        }
        public void Dispose()
        {
            _randDir?.Dispose();
            _randDir = null;
            Endless?.Dispose();
            Endless = null;
        }
    }

    public class EndlessFileStream_PHYSICAL_Tests
    {
        [Fact]
        public void WriteForward_ReadForwardAndCompare()
        {

            int i = 0;
            int maxLines = 100;

            using var w = new WriteEntries(maxLines, 1);
            var e = w.Endless;
            var entry = e.Reader.ReadEntries(FileStreamDirection.Forward, maxLines);
            foreach (var line in entry)
            {
                Assert.Equal(i, Int32.Parse(line.content));
                ++i;
            }
        }

        [Fact]
        public void WriteForward_ReadBackwardsAndCompare()
        {

            int maxLines = 100;

            using var w = new WriteEntries(maxLines, 1);
            var e = w.Endless;
            var entry = e.Reader.ReadEntries(FileStreamDirection.Backwards, maxLines);

            int i = maxLines;

            foreach (var line in entry)
            {
                --i;
                Assert.Equal(i, Int32.Parse(line.content));
            }
        }

        [Fact]
        public void WriteOver1MB_Then_Check_Larger()
        {

            int maxLines = 0;

            using var w = new WriteEntries(maxLines, 0);
            var e = w.Endless;
            var entry = e.Reader.ReadEntries(FileStreamDirection.Backwards, maxLines);

            int i = maxLines;

            foreach (var line in entry)
            {
                if (++i > 100) break;
                Assert.True(Int32.Parse(line.content) > 1000);
            }
        }
    }

}
