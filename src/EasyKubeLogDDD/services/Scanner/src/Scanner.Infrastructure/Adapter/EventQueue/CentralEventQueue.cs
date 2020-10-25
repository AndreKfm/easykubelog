using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Scanner.Domain.Ports;
using SharedKernel;

namespace Scanner.Infrastructure.Adapter.EventQueue
{
    public class CentralEventQueue : IEventProducer, IEventBus, IDisposable
    {

        private CancellationTokenSource? _token;
        private readonly TaskCompletionSource _tcs;
        private Task? _task;
        private ImmutableList<IEventConsumer> _consumerList;
        private readonly Channel<Event> _eventList;

        public CentralEventQueue(int maxEventsInQueue = 1000)
        {
            _consumerList = ImmutableList<IEventConsumer>.Empty;
            _eventList = Channel.CreateBounded<Event>(maxEventsInQueue);
            _tcs = new TaskCompletionSource();
        }

        public IEventProducer GetProducer()
        {
            return this;
        }


        public void Start()
        {
            Stop();
            _token = new CancellationTokenSource();
            _task = Task.Factory.StartNew(() => 
                ConsumeEvents(_token.Token).Wait(), _token.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        public void Stop()
        {
            _token?.Cancel();
            _task?.Wait();
            _token?.Dispose();
            _task?.Dispose();
            _token = null;
            _task = null;
        }

        private async Task ConsumeEvents(CancellationToken token)
        {
            try
            {
                while (token.IsCancellationRequested == false)
                {

                    await _eventList.Reader.WaitToReadAsync(token);
                    if (token.IsCancellationRequested)
                        break;
                    if (!_eventList.Reader.TryRead(out Event? newEvent))
                        break;

                    foreach (var consumer in _consumerList)
                    {
                        consumer.NewEventReceived(newEvent);
                    }
                }
            }
            catch (OperationCanceledException e)
            {
            }
        }

        public void AddConsumer(IEventConsumer consumer)
        {
            _consumerList = _consumerList.Add(consumer);
        }

        public void RemoveConsumer(IEventConsumer consumer)
        {
            _consumerList  = _consumerList.Remove(consumer);
        }


        public async Task PostEvent(Event newEvent)
        {
            for (;;)
            {
                var token = _token?.Token ?? new CancellationToken();
                await _eventList.Writer.WaitToWriteAsync(token);
                if (_eventList.Writer.TryWrite(newEvent))
                    break;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
