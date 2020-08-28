using FileToolsClasses;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WatcherFileListClasses
{




    public class CurrentFileEntry
    {

        public CurrentFileEntry(string fileName, IFile fileStream)
        {
            FileName = fileName;
            CurrentFile = fileStream;
        }
        public string FileName { get; private set; } // Current file name

        public IFile CurrentFile { get; private set; } // Current file name
    }

    public interface ICurrentFileList
    {
        bool AddFile(CurrentFileEntry currentFileEntry); // Returns false if the file exists or fileName is null
    }


    public class WatcherCurrentFileList : ICurrentFileList
    {

        public bool AddFile(CurrentFileEntry currentFileEntry)
        {
            var old = fileList;
            fileList = fileList.Add(currentFileEntry.FileName, currentFileEntry);
            return (old != fileList);
        }

        public bool RemoveFile(string fileName)
        {
            var old = fileList;
            if (!fileList.ContainsKey(fileName))
                return false;
            fileList = fileList.Remove(fileName);
            return (old != fileList);
        }


        public ImmutableDictionary<string /** fileName */, CurrentFileEntry> GetList()
        {
            return fileList;
        }

        ImmutableDictionary<string /** fileName */, CurrentFileEntry> fileList = ImmutableDictionary<string, CurrentFileEntry>.Empty;
    }








    //public class OpenFileWrapper : IOpenFile
    //{
    //    public IFileReadonlyWrapper OpenForRead(string fileName)
    //    {
    //        try
    //        {
    //            var fileStream = File.OpenRead(fileName);
    //            return new FileReadOnlyWrapper(fileStream);
    //        }
    //        catch(Exception)
    //        {

    //        }

    //        return null; // Empty object on any error
    //    }
    //}

}
