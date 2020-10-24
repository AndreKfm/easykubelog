using SharedKernel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Scanner.Domain.Events
{
    public class DirScanBaseEvent : Event
    {
        public DirScanBaseEvent(string directory)
        {
            Directory = directory;
        }
        public override void EnumerateProperties(Action<(string name, string content)> propertyCallback)
        {
            propertyCallback(("Directory", Directory));
        }

        public string Directory { get; init; }

    }
}
