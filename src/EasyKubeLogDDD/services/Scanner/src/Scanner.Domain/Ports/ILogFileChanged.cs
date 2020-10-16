using System;
using System.Collections.Generic;
using System.Text;

namespace Scanner.Domain.Ports
{
    public interface ILogFileChanged
    {
        void LogFileChanged(string logFilePath);
    }
}
