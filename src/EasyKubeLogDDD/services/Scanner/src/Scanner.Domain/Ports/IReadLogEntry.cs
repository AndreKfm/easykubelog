using System;
using System.Collections.Generic;
using System.Text;
using Scanner.Domain.Entities;

namespace Scanner.Domain.Ports
{
    public interface IReadLogEntry
    {
        // Returns default value if no new entry is available
        LogEntry ReadNext();
    }
}
