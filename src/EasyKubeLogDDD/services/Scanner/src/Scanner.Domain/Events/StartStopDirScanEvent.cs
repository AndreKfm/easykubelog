using System;
using System.Collections.Generic;
using System.Text;
using SharedKernel;

namespace Scanner.Domain.Events
{
    public record StartDirScanEvent : DirScanBaseEvent
    {
        public StartDirScanEvent(string directory) : base(directory)
        {
        }
    }

    public record StopDirScanEvent : DirScanBaseEvent
    {
        public StopDirScanEvent(string directory) : base(directory)
        {
        }
    }
}
