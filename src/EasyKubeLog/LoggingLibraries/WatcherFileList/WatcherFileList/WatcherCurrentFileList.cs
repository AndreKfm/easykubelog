using FileToolsClasses;
using System.Collections.Immutable;

namespace WatcherFileListClasses
{




    public class CurrentFileEntry
    {

        public CurrentFileEntry(string fileName, IFile fileStream)
        {
            FileName = fileName;
            CurrentFile = fileStream;
        }
        public string FileName { get; } // Current file name

        public IFile CurrentFile { get; } // Current file name
    }

    public interface ICurrentFileList
    {
        // Returns false if the file exists or fileName is null
    }



    public class WatcherCurrentFileList : ICurrentFileList
    {

        public bool AddFile(CurrentFileEntry currentFileEntry)
        {
            var old = _fileList;
            _fileList = _fileList.Add(currentFileEntry.FileName, currentFileEntry);
            return (old != _fileList);
        }

        public bool RemoveFile(string fileName)
        {
            var old = _fileList;
            if (!_fileList.ContainsKey(fileName))
                return false;
            _fileList = _fileList.Remove(fileName);
            return (old != _fileList);
        }


        public ImmutableDictionary<string, CurrentFileEntry> GetList()
        {
            return _fileList;
        }

        ImmutableDictionary<string, CurrentFileEntry> _fileList = ImmutableDictionary<string, CurrentFileEntry>.Empty;
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
