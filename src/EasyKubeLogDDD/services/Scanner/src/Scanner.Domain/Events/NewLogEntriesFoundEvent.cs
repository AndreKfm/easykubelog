using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scanner.Domain.Entities;
using SharedKernel;


namespace Scanner.Domain.Events
{
    public record NewLogEntriesFoundEvent : Event
    {
    public IReadOnlyList<LogEntry> LogEntries;
    }
}
