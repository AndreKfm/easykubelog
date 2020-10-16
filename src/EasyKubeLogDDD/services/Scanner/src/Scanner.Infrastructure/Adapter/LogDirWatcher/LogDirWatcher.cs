using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scanner.Domain.Ports;

namespace Scanner.Infrastructure.Adapter
{
    public class LogDirWatcher : ILogDirWatcher
    {
        private int _index = 0;


        public LogDirWatcher()
        {
        }


        public void WaitForNextChange(CancellationToken token)
        {
            token.WaitHandle.WaitOne(1000); // Just for simulation purposes
        }



        public string GetChangedFile()
        {
            return $"Simulation{++_index}.log";
        }
    }
}
