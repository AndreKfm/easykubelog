using EasyLogService.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace EasylogService.Test
{
    public class EasyLogServiceSerializationTests
    {

        const string InputLines = @"{""log"":""id: 196801, 01:11:31.051 AM  - ref: ca918362-2afb-4686-9410-a2a10f44764f\n"",""stream"":""stdout"",""time"":""2020-07-06T01:11:31.051244326Z""}
{""log"":""message #1\n"",""stream"":""stdout"",""time"":""2020-07-06T01:11:31.052447769Z""}
{""log"":""id: 196801, 01:11:31.168 AM  - ref: ca918362-2afb-4686-9410-a2a10f44764f\n"",""stream"":""stdout"",""time"":""2020-07-06T01:11:31.16885147Z""}
{""log"":""message #2\n"",""stream"":""stdout"",""time"":""2020-07-06T01:11:31.169748895Z""}
";

        [Fact]
        public void TestDeserialization()
        {
            var lines = InputLines.Split('\n');
            List<KubernetesLogEntry> list = new List<KubernetesLogEntry>();
            foreach (var line in lines)
            {
                if (line.Trim().Length > 0)
                {
                    var parsed = KubernetesLogEntry.Parse(line);
                    Assert.True(parsed.IsDefault == false);
                    list.Add(parsed);
                }
            }

            Assert.Equal("id: 196801, 01:11:31.051 AM  - ref: ca918362-2afb-4686-9410-a2a10f44764f\n", list[0].log);
            Assert.Equal("stdout", list[0].stream);
            Assert.Equal(DateTime.Parse("2020-07-06T01:11:31.051244326Z"), list[0].time.ToLocalTime());

            Assert.Equal("message #2\n", list[3].log);
            Assert.Equal("stdout", list[3].stream);
            var r1 = DateTime.Parse("2020-07-06T01:11:31.16974884Z"); // Json deserializer is one tick different to DateTime parser!!!
            var r2 = list[3].time.ToLocalTime();


            Assert.Equal(r1, r2);
        }
    }
}
