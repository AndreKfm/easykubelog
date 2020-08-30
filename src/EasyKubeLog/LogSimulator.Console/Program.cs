using System;
using System.Collections.Generic;

namespace LogSimulator
{
    class Program
    {
        static void Main(string[] args)
        {

            string directory = (args.Length > 0 && args[0] != String.Empty) ? args[0] : @"C:\\temp\\var\\log\\pods";
            int countFiles = 1;
            if (args.Length > 1)
            {
                if (Int32.TryParse(args[1], out int files))
                    countFiles = files;
            }


            int defaultDelay = 0; // 1000 Milliseconds
            if (args.Length > 2)
            {
                if (Int32.TryParse(args[2], out int delay))
                    defaultDelay = delay;
            }


            Console.WriteLine("Enter any key to stop");
            Console.WriteLine("Start log file generation");



            List<SimulateLogFile> logs = new List<SimulateLogFile>();
            for (int i = 0; i < countFiles; ++i)
            {
                SimulateLogFile s = new SimulateLogFile(directory);
                logs.Add(s);
                s.Start(defaultDelay);
            }

            Console.ReadKey();

            foreach (var log in logs)
            {
                log.Dispose();
            }
        }
    }
}
