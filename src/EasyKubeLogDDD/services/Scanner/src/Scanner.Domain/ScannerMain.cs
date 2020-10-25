using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scanner.Domain.Events;
using Scanner.Domain.Ports;
using Scanner.Domain.Ports.Query;
using SharedKernel;
using SharedKernel.RootInterfaces;

[assembly: InternalsVisibleTo("Scanner.Domain.Test")]
[assembly: InternalsVisibleTo("Scanner.Infrastructure.Test")]
namespace Scanner.Domain
{

    internal class ScannerWatcherExecutor
    {
        private readonly IEventProducer _eventProducer;
        private readonly ILogDirWatcher _watcher;
        private CancellationTokenSource? _source;
        private Task? _watcherTask;

        public ScannerWatcherExecutor(IEventBus eventBus, ILogDirWatcher watcher)
        {
            _eventProducer = eventBus.GetProducer();
            _watcher = watcher;
        }

        public void Start()
        {
            _eventProducer.PostEvent(new StartDirScanEvent(_watcher.GetCurrentDirectory()));

            Stop();
            _source = new CancellationTokenSource();
            _watcherTask = Task.Run(() => ExecuteWatcher(_source.Token));
        }

        public void Stop()
        {
            _source?.Cancel();
            _watcherTask?.Wait();
            _source = null;
            _watcherTask = null;
            _eventProducer.PostEvent(new StopDirScanEvent(_watcher.GetCurrentDirectory()));
        }

        void ExecuteWatcher(CancellationToken token)
        {
            try
            {
                while (token.IsCancellationRequested == false)
                {
                    Task.Delay(TimeSpan.FromSeconds(1), token).Wait(token);
                    if (token.IsCancellationRequested == false)
                    {
                        _eventProducer.PostEvent(new StartDirScanEvent(_watcher.GetCurrentDirectory()));
                        _watcher.ScanDirectory();
                        var changeList = _watcher.GetChangedFiles();
                        if (changeList.Count > 0)
                        {
                            _eventProducer.PostEvent(new FileChangesFoundEvent(_watcher.GetCurrentDirectory(), changeList));
                        }
                        _eventProducer.PostEvent(new DirScanCompletedEvent(_watcher.GetCurrentDirectory()));
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }






    internal class ScannerEventLister : IEventConsumer
    {
        private readonly IScanLogFile _scanner;

        
        public ScannerEventLister(IScanLogFile scanner)
        {
            _scanner = scanner;
        }
        public void NewEventReceived(Event newEvent)
        {
            Console.WriteLine($"New event: {newEvent.Name} ");
            newEvent.EnumerateProperties(((string name, string content) values) =>
                 Console.WriteLine($"  {values.name} = {values.content}"));


            switch (newEvent)
            {
                case FileChangesFoundEvent e:
                {
                    _scanner.ScanLogFiles(e.ChangeList);
                    break;
                }
            }
        }

    }

    public class ScannerMainApplicationRoot : IApplicationMain
    {
        private readonly ILogDirWatcher _watcher;
        private readonly IScanLogFile _scanner;
        private readonly IEventBus _eventBus;

        private ScannerWatcherExecutor? _executor;

        public ScannerMainApplicationRoot(ILogDirWatcher watcher, IScanLogFile scanner, IEventBus eventBus)
        {
            _watcher = watcher;
            _scanner = scanner;
            _eventBus = eventBus;
            ScannerEventLister eventListener = new ScannerEventLister(scanner);
            _eventBus.AddConsumer(eventListener);
            
        }

        public void Start()
        {
            Stop();
            _executor = new ScannerWatcherExecutor(_eventBus, _watcher);
            _executor.Start();
        }

        public void Stop()
        {
            _executor?.Stop();
            _executor = null;
        }
    }
}
