using System;
using System.Collections.Generic;
using System.Text;
using Scanner.Domain.Entities;

namespace Scanner.Domain.Ports
{
    interface IReadLogEntryPort
    {
        // Returns default value if no new entry is available
        LogEntryEntity ReadNext();
    }
}
