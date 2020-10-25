using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Scanner.Domain.Shared;

namespace Scanner.Domain.Events
{
    public class FileChangesFoundEvent : DirScanBaseEvent
    {
        public ReadOnlyCollection<FileEntry> ChangeList { get; }

        public override void EnumerateProperties(Action<(string name, string content)> propertyCallback)
        {
            foreach (var entry in ChangeList)
            {
                propertyCallback(($"FileChange:{entry.FileName}", $"{entry.ChangeType}"));
            }
        }

        public FileChangesFoundEvent(string directory, ReadOnlyCollection<FileEntry> changeList) : base(directory)
        {
            ChangeList = changeList;
        }


    }
}
