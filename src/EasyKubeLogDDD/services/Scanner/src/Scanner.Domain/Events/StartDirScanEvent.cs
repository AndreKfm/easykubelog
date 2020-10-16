using System;
using System.Collections.Generic;
using System.Text;
using SharedKernel;

namespace Scanner.Domain.Events
{
    public class StartDirScanEvent : Event
    {
        public StartDirScanEvent(string directory)
        {
            Directory = directory;
            base.Name = this.GetType().Name;
        }

        public string Directory { get; init; }
    }
}
