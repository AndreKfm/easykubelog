using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scanner.Domain.Events;
using Scanner.Domain.Ports;
using SharedKernel;

[assembly: InternalsVisibleTo("Scanner.Test")]
namespace Scanner.Domain
{

    internal class ScannerWatcherExecutor
    {
        private readonly IEventListener _eventListener;
        private readonly ILogDirWatcher _watcher;
        private CancellationTokenSource? _source;
        private Task? _watcherTask;

        public ScannerWatcherExecutor(IEventListener eventListener, ILogDirWatcher watcher)
        {
            _eventListener = eventListener;
            _watcher = watcher;
        }

        public void Start(ILogFileChanged logfileChanged)
        {
            _eventListener.NewEvent(new StartDirScanEvent("Unknown directory"));

            Stop();
            _source = new CancellationTokenSource();
            _watcherTask = Task.Run(() => ExecuteWatcher(_source.Token, logfileChanged));
        }

        public void Stop()
        {
            _source?.Cancel();
            _watcherTask?.Wait();
            _source = null;
            _watcherTask = null;
        }

        void ExecuteWatcher(CancellationToken token, ILogFileChanged fileChanged)
        {
            try
            {
                while (token.IsCancellationRequested == false)
                {
                    Task.Delay(TimeSpan.FromSeconds(1), token).Wait(token);
                    if (token.IsCancellationRequested == false)
                    {
                        _watcher.ScanDirectory();
                        var changeList = _watcher.GetChangedFiles();
                        foreach (var entry in changeList)
                        {
                            fileChanged.LogFileChanged(entry.FileName);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }


    internal class ScannerEventLister : IEventListener
    {
        public void NewEvent(Event newEvent)
        {
            Console.WriteLine($"New event: {newEvent.Name}");
        }
    }

    public class ScannerMain 
    {
        private readonly ILogDirWatcher _watcher;
        private readonly ILogFileChanged _logFileChanged;

        private ScannerWatcherExecutor? _executor;

        public ScannerMain(ILogDirWatcher watcher, ILogFileChanged logFileChanged)
        {
            _watcher = watcher;
            _logFileChanged = logFileChanged;
        }

        public void Start(ILogFileChanged changed)
        {
            Stop();
            _executor = new ScannerWatcherExecutor(new ScannerEventLister(), _watcher);
            _executor.Start(_logFileChanged);
        }

        public void Stop()
        {
            _executor?.Stop();
            _executor = null;
        }
    }
}
