﻿using EndlessFileStreamClasses;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace FileArrayConsole
{



    class Program
    {

        static void ReadWhileWrite()
        {
            var stream = new EndlessFileStream(new EndlessFileStreamSettings { BaseDirectory = @"C:\test\FileArray", MaxLogFileSizeInMByte = 1 });
            Task w = Task.Run(() => TestWritingAndPerformance(stream));

            for (; ; )
            {
                try
                {
                    var entries = stream.Reader.ReadEntries(10);
                    foreach (var a in entries)
                    {
                        Console.WriteLine($"READ: {a}");
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Exception while reading endless stream: {e.Message}");
                }

                Task.Delay(1000).Wait();
            }


        }


        static void TestWritingAndPerformance(EndlessFileStream fileStream = null)
        {
            var list = fileStream ?? new EndlessFileStream(new EndlessFileStreamSettings { BaseDirectory = @"C:\test\FileArray", MaxLogFileSizeInMByte = 1 });
            long size = 0;
            Stopwatch w = Stopwatch.StartNew();
            int index = 0;
            for (; ; )
            {
                string entry = Guid.NewGuid().ToString() + ":" + (++index).ToString();
                list.Writer.WriteToFileStream(entry);
                size += entry.Length; // Roughly - string in utf8 might be different to byte in size
                if (w.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine($"{(double)size / (double)w.ElapsedMilliseconds * 1000.0 / 1024.0 / 1024.0} MB/s");
                    Task.Delay(100).Wait();
                    w = Stopwatch.StartNew();
                    size = 0;
                }
            }
        }
        static void Main(string[] args)
        {

            //b.GenerateOutputFile(@"C:\test\xlogtest", @"c:\test\central_test.log");
            //b.GenerateOutputFile(@"c:\test\logs", @"c:\test\central_test.log");
            //EndlessFileStreamBuilder b = new EndlessFileStreamBuilder(); b.GenerateEndlessFileStream(@"c:\test\logs", @"C:\test\endless");
            //EndlessFileStreamBuilder b = new EndlessFileStreamBuilder(); b.GenerateEndlessFileStream(new EndlessFileStreamSettings { BaseDirectory = @"C:\test\endless" }, @"C:\temp\var\log\pods");
            EndlessFileStreamBuilder b = new EndlessFileStreamBuilder(); b.GenerateEndlessFileStream(new EndlessFileStreamSettings { BaseDirectory = @"C:\test\endless" }, @"c:\test\logs");

            return; 
            EndlessFileStream e = new EndlessFileStream(new EndlessFileStreamSettings { BaseDirectory = @"C:\test\endless", MaxLogFileSizeInMByte = 1024 });
            var stream = e.Reader.ReadEntries(int.MaxValue);
            string search = "gonzo";
            Stopwatch w = Stopwatch.StartNew();
            int count = 0;
            foreach (var line in stream)
            {
                //Console.WriteLine(line);
                if (line.content.Contains(search, StringComparison.OrdinalIgnoreCase))
                {
                    ++count;
                    Console.WriteLine($"[{count}] found");
                }
            }

            Console.WriteLine($"Needed {w.ElapsedMilliseconds} ms");

            count = 0; w = Stopwatch.StartNew();
            Parallel.ForEach(stream, (line) =>
            {
                {
                    //Console.WriteLine(line);
                    if (line.content.Contains(search, StringComparison.OrdinalIgnoreCase))
                    {
                        ++count;
                        Console.WriteLine($"[{count}] found");
                    }
                }
            });

            Console.WriteLine($"Needed {w.ElapsedMilliseconds} ms");
            return;

            //TestWritingAndPerformance();
            ReadWhileWrite();
        }
    }
}
