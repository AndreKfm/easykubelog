using EasyLogService.Services.CentralLogService;
using LogEntries;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasyLogService.Tool.Simulator
{
    public class LogSimulatorReadAllContent : IDisposable
    {
        public LogSimulatorReadAllContent()
        {

        }

        private bool readDone = false;
        private readonly Task current = Task.CompletedTask;
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        public void InitialRead(string directory, ICentralLogServiceCache cache, int maxLinesToRead = 1000)
        {
            if (readDone)
                return;

            current.ContinueWith((task) =>
            {
                var token = tokenSource.Token;
                if (token.IsCancellationRequested)
                    return;

                var files = Directory.GetFiles(directory);

                Console.WriteLine($"Read simulation files from [{directory}]");
                Parallel.ForEach(files, (file) =>
                {
                    if (token.IsCancellationRequested)
                        return;
                    var lines = File.ReadAllLines(file);
                    if (maxLinesToRead != -1)
                    {
                        foreach (var line in lines.Take(maxLinesToRead))
                        {
                            if (token.IsCancellationRequested)
                                return;
                            cache.AddEntry(new LogEntry(file, line));
                        }
                    }
                    else foreach (var line in lines)
                        {
                            if (token.IsCancellationRequested)
                                return;
                            cache.AddEntry(new LogEntry(file, line));
                        }
                });

                readDone = true;
            });
        }

        public void Dispose()
        {
            tokenSource.Cancel();
        }
    }
}
