using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel;

namespace Scanner.Domain.Events
{
    public record CheckLogFileStart : Event
    {
        public CheckLogFileStart(string fileName)
        {
            FileName = fileName;
        }
        public string FileName { get; }
    }
}
