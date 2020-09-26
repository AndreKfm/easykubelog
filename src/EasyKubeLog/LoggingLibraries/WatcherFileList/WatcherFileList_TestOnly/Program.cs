using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DirectoryWatcher;
using Microsoft.Extensions.Options;
using WatcherFileListClasses;
// ReSharper disable All

namespace WatcherFileList.Console
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
                System.Console.WriteLine($"XXX: {output.FileName} {output.Lines}");
                //return AutoCurrentFileList.ReadAsyncOperation.ContinueRead;
            });
            System.Console.WriteLine("CurrentFileListTest wait...");
            System.Console.ReadLine();
            a.Stop();

            Task.Delay(1000).Wait();

        }

        static void Main(string[] args)
        {

            var consoleTracer = new ConsoleTraceListener(true);
            Trace.Listeners.Add(consoleTracer);
            consoleTracer.Name = "ManualFileSystemWatcherTrace";



            string directory = (args.Length > 0 && args[0] != String.Empty) ? args[0] : @"C:\test\deleteme\logtest";

            // ReSharper disable once RedundantJumpStatement
            CurrentFileListTest(directory); return;

/*
            WatcherFileListClasses.WatcherFileList w = new WatcherFileListClasses.WatcherFileList(new FileDirectoryWatcherSettings { }, null, 15000);
            w.Start((list) =>
            {
                foreach (var e in list)
                {
                    System.Console.WriteLine($"{e.FileName} : {e.LastChanges}");
                }

            });

            System.Console.WriteLine("Waiting");
            System.Console.ReadLine();
*/
        }
    }
}
