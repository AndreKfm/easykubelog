using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedKernel;

namespace Scanner.Domain.Ports
{


    public interface IEventBus : IDisposable
    {
        IEventProducer GetProducer();
        void AddConsumer(IEventConsumer consumer);
        void RemoveConsumer(IEventConsumer consumer);
        void Start();
        void Stop();
    }

    public interface IEventConsumer
    {
        void NewEventReceived(Event newEvent);
    }

    public interface IEventProducer
    {
        // Task will block if max number of events are reached
        Task PostEvent(Event newEvent);
    }

}
