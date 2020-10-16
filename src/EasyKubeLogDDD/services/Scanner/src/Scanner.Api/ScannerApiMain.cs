using System;
using System.Collections.Generic;
using System.Text;
using Scanner.Api.Services;
using Scanner.Domain.Ports;

namespace Scanner.Api
{
    public class ScannerApiMain : IScannerMain
    {
        private readonly ILogDirWatcher _logDirWatcher;

        public ScannerApiMain(ILogDirWatcher logDirWatcher)
        {
            _logDirWatcher = logDirWatcher;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}
