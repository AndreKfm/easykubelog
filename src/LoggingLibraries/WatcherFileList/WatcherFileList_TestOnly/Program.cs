using DirectoryWatcher;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;
using WatcherFileListClasses;

namespace WatcherFileListClasses_TestOnly
{
    class Program
    {
        
        static void CurrentFileListTest(string directory)
        {
            var settings = Options.Create(new FileDirectoryWatcherSettings());
            AutoCurrentFileList a = new AutoCurrentFileList(settings);
            a.Start();
            var task = a.BlockingReadAsyncNewOutput((output, token) =>
            {
                Console.WriteLine($"XXX: {output.FileName} {output.Lines}");
                //return AutoCurrentFileList.ReadAsyncOperation.ContinueRead;
            });
            Console.WriteLine("CurrentFileListTest wait...");
            Console.ReadLine();
            a.Stop();

            Task.Delay(1000).Wait();

        }

        static void Main(string[] args)
        {
            var consoleTracer = new ConsoleTraceListener(true);
            Trace.Listeners.Add(consoleTracer);
            consoleTracer.Name = "ManualFileSystemWatcherTrace";



            string directory = (args.Length > 0 && args[0] != String.Empty) ? args[0] : @"C:\test\deleteme\logtest";

            CurrentFileListTest(directory); return; 

            WatcherFileList w = new WatcherFileList(new FileDirectoryWatcherSettings { }, null, 15000);
            w.Start((list) =>
            {
                foreach (var e in list)
                {
                    Console.WriteLine($"{e.FileName} : {e.LastChanges}");
                }

            });

            Console.WriteLine("Waiting");
            Console.ReadLine();
        }
    }
}
