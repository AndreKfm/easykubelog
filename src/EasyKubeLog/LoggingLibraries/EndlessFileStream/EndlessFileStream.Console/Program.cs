﻿using FileToolsClasses;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EndlessFileStream;
// ReSharper disable All

namespace FileArrayConsole
{



    class Program
    {

        static void ReadWhileWrite()
        {
            var stream = new EndlessFileStream.EndlessFileStream(new EndlessFileStreamSettings { BaseDirectory = @"C:\test\FileArray", MaxLogFileSizeInMByte = 1 });
            Task w = Task.Run(() => TestWritingAndPerformance(stream));

            Stopwatch watch = Stopwatch.StartNew();
            int maxExecutionTimeMinutes = 15;
            Console.WriteLine($"Running now for {maxExecutionTimeMinutes}");

            for (; ; )
            {
                try
                {
                    var entries = stream.Reader.ReadEntries(FileStreamDirection.Forward, 10);
                    foreach (var a in entries)
                    {
                        Console.WriteLine($"READ: {a}");
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Exception while reading endless stream: {e.Message}");
                }

                if (watch.Elapsed.Minutes > maxExecutionTimeMinutes)
                    break;
            }


            // ReSharper disable once FunctionNeverReturns
        }


        static void TestWritingAndPerformance(EndlessFileStream.EndlessFileStream fileStream = null)
        {
            var list = fileStream ?? new EndlessFileStream.EndlessFileStream(new EndlessFileStreamSettings { BaseDirectory = @"C:\test\FileArray", MaxLogFileSizeInMByte = 1 });
            long size = 0;
            Stopwatch w = Stopwatch.StartNew();
            int index = 0;

            int maxExecutionTimeMinutes = 15; 
            Console.WriteLine($"Running now for {maxExecutionTimeMinutes}");
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

                if (w.Elapsed.Minutes > maxExecutionTimeMinutes)
                    break;
            }
        }
        static void Main(string[] args)
        {

            //b.GenerateOutputFile(@"C:\test\xlogtest", @"c:\test\central_test.log");
            //b.GenerateOutputFile(@"c:\test\logs", @"c:\test\central_test.log");
            //EndlessFileStreamBuilder b = new EndlessFileStreamBuilder(); b.GenerateEndlessFileStream(@"c:\test\logs", @"C:\test\endless");
            //EndlessFileStreamBuilder b = new EndlessFileStreamBuilder(); b.GenerateEndlessFileStream(new EndlessFileStreamSettings { BaseDirectory = @"C:\test\endless" }, @"C:\temp\var\log\pods");
            EndlessFileStreamBuilder b = new EndlessFileStreamBuilder(); b.GenerateEndlessFileStream(new EndlessFileStreamSettings { BaseDirectory = @"C:\test\endless" }, @"c:\test\logs");

            // ReSharper disable once RedundantJumpStatement
            return;
/*
            EndlessFileStream.EndlessFileStream e = new EndlessFileStream.EndlessFileStream(new EndlessFileStreamSettings { BaseDirectory = @"C:\test\endless", MaxLogFileSizeInMByte = 1024 });
            var stream = e.Reader.ReadEntries(FileStreamDirection.Forward, int.MaxValue);
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
*/
        }
    }
}
