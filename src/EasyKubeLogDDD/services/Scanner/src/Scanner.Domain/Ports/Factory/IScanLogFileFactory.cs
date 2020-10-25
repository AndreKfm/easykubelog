using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scanner.Domain.Ports.Query;

namespace Scanner.Domain.Ports.Factory
{
    public interface IScanLogFileFactory
    {
        public IScanLogFile CreateScanLogFile(IEventBus listener);
    }
}
