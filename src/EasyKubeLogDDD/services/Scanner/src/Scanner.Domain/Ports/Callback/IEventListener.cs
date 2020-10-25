using System;
using System.Collections.Generic;
using System.Text;
using SharedKernel;

namespace Scanner.Domain.Ports
{
    public interface IEventListener
    {
        void NewEvent(Event newEvent);
    }
}
