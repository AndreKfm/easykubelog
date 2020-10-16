using System;
using System.Collections.Generic;
using System.Text;
using SharedKernel;

namespace Scanner.Domain.Ports
{
    interface IEventListener
    {
        void NewEvent(Event newEvent);
    }
}
