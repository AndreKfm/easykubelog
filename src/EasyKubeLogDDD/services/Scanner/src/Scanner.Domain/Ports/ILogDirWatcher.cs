using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Scanner.Domain.Ports
{


    public interface ILogDirWatcher
    {
        public void WaitForNextChange(CancellationToken token);
        public string GetChangedFile();

    }
}
