using EasyLogService.Services;
using EasyLogService.Services.CentralLogService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EasyLogService.Tool.Simulator
{
    public class LogSimulatorReadAllContent
    {
        public LogSimulatorReadAllContent()
        {

        }

        private bool readDone = false;
        public void InitialRead(string directory, ICentralLogServiceCache cache, int maxLinesToRead = 1000)
        {
            if (readDone)
                return;

            var files = Directory.GetFiles(directory);

            Console.WriteLine($"Read simulation files from [{directory}]");
            Parallel.ForEach(files, (file) =>
            {
                var lines = File.ReadAllLines(file);
                foreach (var line in lines.Take(maxLinesToRead))
                {
                    cache.AddEntry(new LogEntry(file, line));
                }
            });

            readDone = true;
        }
    }
}
