using LogEntries;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace LogEntries.Test
{
    public class LogEntriesTests
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
                    var parsed = KubernetesLogEntry.ParseFromContainer(line);
                    Assert.True(parsed.IsDefault() == false);
                    list.Add(parsed);
                }
            }

            Assert.Equal("id: 196801, 01:11:31.051 AM  - ref: ca918362-2afb-4686-9410-a2a10f44764f\n", list[0].Log);
            Assert.Equal("stdout", list[0].Stream);
            Assert.Equal(DateTimeOffset.Parse("2020-07-06T01:11:31.051244326Z"), list[0].Time.ToLocalTime());

            Assert.Equal("message #2\n", list[3].Log);
            Assert.Equal("stdout", list[3].Stream);
            var r1 = DateTimeOffset.Parse("2020-07-06T01:11:31.169748895Z"); // Json deserializer is one tick different to DateTime parser!!!
            var r2 = list[3].Time.ToUniversalTime();


            Assert.Equal(r1, r2);

        }

        [Fact]
        public void DeserializeContainerName()
        {
            //string name = "kube-apiserver-myserver-2_kube-system_kube-apiserver-1827c8c0196e15c01ed339eac252aa483212dfd1b25ce44d2fca974a954c196b";
            string name = "default_loga1_e79b891a-c8c5-4041-b9f8-42edb2dcb268\\loga1\\0.log";

            var value = KubernetesContainerNameTools.DeserializeContainerNameSimple(name);

            //Assert.Equal("default", value.deployment);
            Assert.Equal("default", value.nm);
            Assert.Equal("loga1", value.containerName);
            Assert.Equal("e79b891a-c8c5-4041-b9f8-42edb2dcb268", value.podId);
        }

        [Fact]
        public void DeserializeContainerNameFromContainerLog()
        {
            string name = "kube-apiserver-myserver-2_kube-system_kube-apiserver-1827c8c0196e15c01ed339eac252aa483212dfd1b25ce44d2fca974a954c196b";

            var value = KubernetesContainerNameTools.DeserializeContainerName(name);

            Assert.Equal("kube-apiserver-myserver-2", value.deployment);
            Assert.Equal("kube-system", value.nm);
            Assert.Equal("kube-apiserver", value.containerName);
            Assert.Equal("1827c8c0196e15c01ed339eac252aa483212dfd1b25ce44d2fca974a954c196b", value.contId);
        }

        [Fact]
        public void DeserializeContainerNameFromKubernetesLogFromContainerLog()
        {
            string name = "kube-apiserver-myserver-2_kube-system_kube-apiserver-1827c8c0196e15c01ed339eac252aa483212dfd1b25ce44d2fca974a954c196b.log";
            string log = @"{ ""log"":"""",""stream"":"""",""time"":""0001-01-01T00:00:00+00:00""}"; // Dummy not needed directly
            var k = KubernetesLogEntry.ParseFromContainer(log, name);
            var value = KubernetesContainerNameTools.DeserializeContainerName(k.Container);

            Assert.Equal("kube-apiserver-myserver-2", value.deployment);
            Assert.Equal("kube-system", value.nm);
            Assert.Equal("kube-apiserver", value.containerName);
            Assert.Equal("1827c8c0196e15c01ed339eac252aa483212dfd1b25ce44d2fca974a954c196b", value.contId);
        }


        [Fact]
        public void DeserializeContainerNameFromKubernetesLogFromPodLog()
        {
            string name = "default_loga1_e79b891a-c8c5-4041-b9f8-42edb2dcb268\\loga1\\0.log";
            var log = "2020-08-09T19:19:48.670551Z stdout F root@xxx:/# echo ##";
            var k = KubernetesLogEntry.Parse(log, name);
            var value = KubernetesContainerNameTools.DeserializeContainerNameSimple(k.Container);

            Assert.Equal("default", value.nm);
            Assert.Equal("loga1", value.containerName);
            Assert.Equal("e79b891a-c8c5-4041-b9f8-42edb2dcb268", value.podId);
        }
    }
}
