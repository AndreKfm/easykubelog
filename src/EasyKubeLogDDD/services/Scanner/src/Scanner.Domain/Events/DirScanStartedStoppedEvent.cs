using System;
using System.Collections.Generic;
using System.Text;
using SharedKernel;

namespace Scanner.Domain.Events
{
    public class DirScanStartedEvent : DirScanBaseEvent
    {
        public DirScanStartedEvent(string directory) : base(directory)
        {
            
        }
    }

    public class DirScanCompletedEvent : DirScanBaseEvent
    {
        public DirScanCompletedEvent(string directory) : base(directory)
        {

        }
    }
}
