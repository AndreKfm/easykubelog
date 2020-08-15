using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using WatcherFileListClasses;
using Xunit;

namespace WatcherCurrentFileListClasses
{
    public class WatcherCurrentFileLists_BackwardsSeekTests_PHYSICAL_FILE_
    {
        private Random _rand = new Random();

        string CreateRandomString(int len)
        {
            var builder = new StringBuilder(len);
            for (int i = 0; i < len; ++i)
            {
                builder.Append((char)_rand.Next('A', 'z'));
            }
            return builder.ToString();
        }

        string SimulateRandomLogEntry(int len)
        {
            // We don't care here about original and correct docker date time format
            const string LogFormatDocker = @"{""log"":""#MSG#"",""stream"":""stdout"",""time"":""#DATE#""}";

            return LogFormatDocker.Replace("#MSG#", CreateRandomString(len)).Replace("#DATE#", DateTime.Now.ToString());
        }

        Span<byte> SimulateRandomLogEntryAsByteArray(int len)
        {
            string temp = SimulateRandomLogEntry(len);
            return System.Text.Encoding.Default.GetBytes(temp);
        }

        (IFileStream stream, string path) CreateFile()
        {
            string path = Path.Combine(Path.GetTempPath(), "WatcherCurrentFileLists_BackwardsSeekTests_PHYSICAL_FILE_" + Guid.NewGuid().ToString() + ".txt");
            var wrapper = new FileStreamWrapper(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            return (wrapper, path);
        }

        void WriteRandomStringAndAddToList(List<(string line, long position)> writtenEntries, int lenToWrite, IFileStreamWriter writer)
        {
            var log = SimulateRandomLogEntry(lenToWrite);
            writtenEntries.Add((log, ((IFileStream)writer).Position));
            log += Environment.NewLine;
            writer.Write(System.Text.Encoding.Default.GetBytes(log));
        }

        [Fact]
        public void CreateFileAndSeekBackwardsWithFileSeeker()
        {
            var file = CreateFile();
            try
            {
                var stream = file.stream;
                var writer = (IFileStreamWriter)stream;

                List<(string line, long position)> writtenEntries = new List<(string line, long position)>();

                int[] writeLen = { 1, 190, 80, 40, 79, 120, 81, 80, 79, 2, 160, 161, 240, 0, 234 };
                foreach (var len in writeLen)
                {
                    WriteRandomStringAndAddToList(writtenEntries, len, writer);
                }

                foreach (var l in writtenEntries)
                {
                    Console.WriteLine($"{l.line}");
                }
                Console.WriteLine();

                foreach (var l in writtenEntries)
                {
                    Console.Write($" {l.position}, ");
                }
                Console.WriteLine();
                // Now reread everything and check if we read correctly backwards
                FileSeeker seeker = new FileSeeker();
                foreach (var shouldBe in Enumerable.Reverse(writtenEntries.AsEnumerable()))
                {
                    var line = seeker.SeekLastLineFromCurrentAndPositionOnStartOfItAndReturnReadLine(stream);
                    Assert.Equal(shouldBe.line, line);
                }

            }
            finally
            {
                file.stream.Dispose();
                File.Delete(file.path);
            }
        }
    }
}
