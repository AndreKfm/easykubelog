using DirectoryWatching;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileSystemWatcher_TestOnly
{
    class Program
    {

        static void Output(string output)
        {
            Console.WriteLine(output);
        }

        static void TestThrottling()
        {
            SemaphoreSlim slim = new SemaphoreSlim(1);
            CancellationTokenSource source = new CancellationTokenSource();


            int ticker = 0;
            for (; ; )
            {
                var token = source.Token;
                if (slim.WaitAsync(0).Result == true)
                {
                    Task t = Task.Run(async () =>
                    {
                        try
                        {
                            Output("From inside slim");
                            await Task.Delay(2000, token);
                            if (token.IsCancellationRequested)
                            {
                                Output("!! Cancelled !!");
                                return;
                            }
                            else 
                            {
                                Output("## NOT Cancelled ##");
                                return;
                            }
                        }
                        catch(Exception)
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
                Task.Delay(400).Wait();
                if (++ticker > 3)
                {
                    source.Cancel();
                    break;
                }
            }

            Output("END");

            Console.ReadLine();

        }

        static void Main(string[] args)
        {

            string directory = (args.Length > 0 && args[0] != String.Empty) ? args[0] : @"C:\test\deleteme\xwatchertest";

            DirectoryWatcher w = new DirectoryWatcher();
            w.Open(directory, new FilterAndCallbackArgument("*.txt", 
                (object sender, WatcherCallbackArgs args) =>
                {
                    Console.WriteLine($"{args.ChangeType} {args.FileName}");
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
            Console.ReadLine();

        }

        private static void Watcher_Disposed(object sender, EventArgs e)
        {
            Console.WriteLine($"Watcher disposed: {e}");
        }

        private static void Watcher_Error(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"Watcher error: {e.GetException()} : {e}");
        }

        private static void Watcher_Callback(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Watcher callback: {e.FullPath} : {e.ChangeType} : {e.Name}");
        }
    }
}
