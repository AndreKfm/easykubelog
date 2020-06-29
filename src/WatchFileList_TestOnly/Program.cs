using System;
using WatchedFileList;

namespace WatchFileList_TestOnly
{
    class Program
    {
        static void Main(string[] args)
        {
            string directory = (args.Length > 0 && args[0] != String.Empty) ? args[0] : @"C:\test\deleteme\xwatchertest";
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
