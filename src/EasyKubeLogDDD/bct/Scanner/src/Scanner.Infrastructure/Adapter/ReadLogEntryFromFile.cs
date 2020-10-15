using System;
using System.Collections.Generic;
using System.Text;
using Scanner.Domain.Entities;
using Scanner.Domain.Ports;

namespace Scanner.Infrastructure.Adapter
{
    class ReadLogEntryFromFile : IReadLogEntryPort
    {
        public LogEntry ReadNext()
        {
            // Generate demo entries for now 
            return new LogEntry { ModuleName = "DEMO ENTITY", Content = $"DEMO ENTITY: {++index}", CreateTime = DateTimeOffset.Now};
        }

        private int index = 0;
    }
}
