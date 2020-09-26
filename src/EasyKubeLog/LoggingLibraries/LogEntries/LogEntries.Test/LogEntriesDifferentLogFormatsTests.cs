using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace LogEntries.Test
{
    public class LogEntriesDifferentLogFormatsTests
    {

        const string LogFormatDocker = @"{""log"":""message #2\n"",""stream"":""stdout"",""time"":""2020-07-06T01:11:31.169748895Z""}";
        const string LogFormatContainerD = @"2020-08-09T16:19:55.3300942Z stdout F loga#26";
        const string LogFormatContainerDInvalid1 = @"2020-08-09T16:19:55.3300942Z";
        const string LogFormatContainerDInvalid2 = @"      2020-08-09T16:19:55.3300942Z sss";
        const string LogFormatContainerDInvalid3 = @"2020-08-09T16:19:55.3300942Z stdout";
        const string LogFormatContainerDInvalid4 = @"2020-08-09T16:19:55.3300942Z stdoutF loga#26";

        [Fact]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public void TestContainerdParser()
        {
            LogParserContainerd contd = new LogParserContainerd();
            var parsed = contd.ParseLine(LogFormatContainerD);
            Assert.NotEqual(default, parsed);
            Assert.Equal("loga#26", parsed.Line);
            Assert.NotEqual(" loga#26", parsed.Line);
            Assert.NotEqual("loga#26 ", parsed.Line);
            Assert.Equal("stdout", parsed.Stream);
        }

        [Fact]
        public void TestContainerdParserInvalidFormats()
        {
            LogParserContainerd contd = new LogParserContainerd();
            var parsed1 = contd.ParseLine(LogFormatContainerDInvalid1);
            var parsed2 = contd.ParseLine(LogFormatContainerDInvalid2);
            var parsed3 = contd.ParseLine(LogFormatContainerDInvalid3);
            var parsed4 = contd.ParseLine(LogFormatContainerDInvalid4);
            Assert.Equal(default, parsed1);
            Assert.Equal(default, parsed2);
            Assert.Equal(default, parsed3);
            Assert.Equal(default, parsed4);
        }

        [Fact]
        public void AutoDetectLogFormat()
        {
            var dockerParser = LogParserAutoDetect.GetAutoParser(LogFormatDocker);
            Assert.NotNull(dockerParser);

            var containerDParser = LogParserAutoDetect.GetAutoParser(LogFormatContainerD);
            Assert.NotNull(containerDParser);

            var invalidParser = LogParserAutoDetect.GetAutoParser(LogFormatContainerDInvalid4);
            Assert.Null(invalidParser);

            var invalidParser2 = LogParserAutoDetect.GetAutoParser(LogFormatContainerDInvalid2);
            Assert.Null(invalidParser2);
        }

    }
}
