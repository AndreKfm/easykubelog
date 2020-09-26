using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DirectoryWatcher;
// ReSharper disable All

namespace FileSystemWatcher.Console
{
    class Program
    {

        static void Output(string output)
        {
            System.Console.WriteLine(output);
        }

        static void TestThrottling()
        {
            SemaphoreSlim slim = new SemaphoreSlim(1);
            CancellationTokenSource source = new CancellationTokenSource();


            int ticker = 0;
            for (; ; )
            {
                var token = source.Token;
                if (slim.WaitAsync(0, token).Result)
                {
                    var token1 = token;
                    Task t = Task.Run(async () =>
                    {
                        try
                        {
                            Output("From inside slim");
                            await Task.Delay(2000, token1);
                            Output(token1.IsCancellationRequested ? "!! Cancelled !!" : "## NOT Cancelled ##");
                        }
                        catch (Exception)
                        {
                            Output("Exception");
                        }
                        finally
                        {
                            Output("From inside release slim");
                            slim.Release();
                        }
                    }, token);
                }

                Output("From outside");
                Task.Delay(400, token).Wait(token);
                if (++ticker > 3)
                {
                    source.Cancel();
                    break;
                }
            }

            Output("END");

            System.Console.ReadLine();

        }

        static void Main(string[] args)
        {

            //var dir = @"/mnt/c/test/deleteme/xwatchertest/";
            var dir = @"/home/dev/log";
            //var dir = @"/tmp/log";

            //string directory = (args.Length > 0 && args[0] != String.Empty) ? args[0] : @"C:\test\deleteme\xwatchertest";
            string directory = (args.Length > 0 && args[0] != String.Empty) ? args[0] : dir;

            System.Console.WriteLine($"Watching now directory: {directory}");

            Stopwatch watch = Stopwatch.StartNew();
            int maxExecutionTimeMinutes = 15;
            System.Console.WriteLine($"Running now for {maxExecutionTimeMinutes}");

            Task.Run(() =>
            {
                int n = 0;
                string ldir = "/tmp/log/";
                for (; ; )
                {
                    Task.Delay(1000).Wait();
                    File.WriteAllText($"{ldir}linkhard.txt", $"hello{++n}");
                    File.WriteAllText($"{ldir}linksoft.txt", $"hello{++n}");
                    System.Console.Write('.');
                    //File.WriteAllText($"{dir}linkhard.txt", $"hello{++n}");
                    if (watch.Elapsed.Minutes > maxExecutionTimeMinutes)
                        break;
                }

            });

            FileDirectoryWatcher w = new FileDirectoryWatcher(new FileDirectoryWatcherSettings(directory));
            w.Open(new FilterAndCallbackArgument(String.Empty,
                (sender, watcherCallbackArgs) =>
                {
                    System.Console.WriteLine($"{watcherCallbackArgs.ChangeType} {watcherCallbackArgs.FileName}");
                }
                ));


            //FileSystemWatcher watcher = new FileSystemWatcher(@"C:\test\deleteme\xwatchertest", "*.txt");


            //watcher.Changed += Watcher_Callback;
            //watcher.Created += Watcher_Callback;
            //watcher.Deleted += Watcher_Callback; ;
            //watcher.Renamed += Watcher_Callback;
            //watcher.Error += Watcher_Error; ;
            //watcher.Disposed += Watcher_Disposed;
            //watcher.IncludeSubdirectories = false;
            //watcher.InternalBufferSize = 65536; // Reserve for a larger number of containers running


            //watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            //watcher.EnableRaisingEvents = true; // Must be set always
            System.Console.ReadLine();

        }

        private static void Watcher_Disposed(object sender, EventArgs e)
        {
            System.Console.WriteLine($"Watcher disposed: {e}");
        }

        private static void Watcher_Error(object sender, ErrorEventArgs e)
        {
            System.Console.WriteLine($"Watcher error: {e.GetException()} : {e}");
        }

        private static void Watcher_Callback(object sender, FileSystemEventArgs e)
        {
            System.Console.WriteLine($"Watcher callback: {e.FullPath} : {e.ChangeType} : {e.Name}");
        }
    }
}
