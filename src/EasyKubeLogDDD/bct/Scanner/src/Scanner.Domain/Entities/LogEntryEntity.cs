using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

namespace Scanner.Domain.Entities
{
    internal class LogEntryEntity
    {
        public string Content { get; set; }
        public DateTimeOffset CreateTime { get; set; }
        public string ModuleName { get; set; }


        private bool IsValid => ModuleName != default && Content != default && ModuleName != default;
    }
}
