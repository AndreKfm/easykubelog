using System;
using System.Threading.Tasks;
using WatchedFileList;

namespace WatchFileList_TestOnly
{
    class Program
    {
        
        static void CurrentFileListTest(string directory)
        {
            AutoCurrentFileList a = new AutoCurrentFileList();
            a.Start(directory);
            var task = a.BlockingReadAsyncNewOutput((output, token) =>
            {
                Console.WriteLine($"XXX: {output.Filename} {output.Lines}");
                Task.Delay(100000).Wait();
                //return AutoCurrentFileList.ReadAsyncOperation.ContinueRead;
            });
            Console.WriteLine("CurrentFileListTest wait...");
            Console.ReadLine();
            a.Stop();

            Task.Delay(1000).Wait();

        }

        static void Main(string[] args)
        {

            string directory = (args.Length > 0 && args[0] != String.Empty) ? args[0] : @"C:\test\deleteme\xwatchertest";

            CurrentFileListTest(directory); return; 

            WatchFileList w = new WatchFileList(directory, null, 15000);
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
