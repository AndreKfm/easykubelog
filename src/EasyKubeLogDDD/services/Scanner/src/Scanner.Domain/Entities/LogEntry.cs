using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using SharedKernel;


namespace Scanner.Domain.Entities
{

    public class LogEntry : Entity
    {
        public LogEntry(DateTimeOffset createTime, string content, string moduleName)
        {
            CreateTime = createTime;
            Content = Content;
            ModuleName = moduleName;
        }

        public LogEntry()
        {
        }

        public string Content { get; init; }
        public DateTimeOffset CreateTime { get; init; }
        public string ModuleName { get; init;  }


        private bool IsEmpty => ModuleName == default && Content == default && ModuleName == default;
    }
}
