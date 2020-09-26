using EasyKubeLogService.Services.CentralLogService;
using LogEntries;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasyKubeLogService.Tool.Simulator
{
    public class LogSimulatorReadAllContent : IDisposable
    {
        private bool _readDone;
        private readonly Task _current = Task.CompletedTask;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public void InitialRead(string directory, ICentralLogServiceCache cache, int maxLinesToRead = 1000)
        {
            if (_readDone)
                return;

            _current.ContinueWith((task) =>
            {
                var token = _tokenSource.Token;
                if (token.IsCancellationRequested)
                    return;

                var files = Directory.GetFiles(directory);

                // ReSharper disable once LocalizableElement
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

                _readDone = true;
            });
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
        }
    }
}