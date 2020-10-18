using System;
using System.Collections.Generic;
using System.IO;
using FileList = System.Collections.Generic.Dictionary<string, long>;

namespace Scanner.Infrastructure.Adapter.LogDirWatcher.ManualDirectoryScan
{



  //using FileListEntry = (string fileName, DateTime lastWriteUtc, long fileLength);


  /// <summary>
    /// Holds a list of files without paths and their current read / write offset
    /// (interpretation of offset must be done by other classes - only one offset will be held)
    /// Path will be simple truncated - there is not verification of the path.
    /// If a file from another directory with the same file name will be tried to be added, 
    /// only one instance will be held 
    /// </summary>
    public class ManualScanPhysicalFileSystemWatcherFileList
    {
        // Holds file names [without (root) path] and the latest known file position
        private readonly Dictionary<string, (DateTime lastWriteUtc, long fileLen)> _files = new Dictionary<string, (DateTime lastWriteUtc, long fileLen)>();

        private readonly Func<string, string?> _normalizeFileName;

        public ManualScanPhysicalFileSystemWatcherFileList(ManualScanPhysicalFileSystemWatcherFileListSettings settings)
        {
            settings ??= new ManualScanPhysicalFileSystemWatcherFileListSettings();
            _normalizeFileName = settings.NormalizeFileName;
        }

        private string NormalizeString(string path)
        {
            return _normalizeFileName(path) ?? String.Empty;
        }

        public bool AddFileTruncPath(string fileName, DateTime initial = default, long fileLen = long.MinValue)
        {
            try
            {
                fileName = NormalizeString(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
                if (_files.ContainsKey(fileName))
                    return false;
                _files.Add(fileName, (initial.ToUniversalTime(), fileLen));
                return true;
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        public bool RemoveFileIgnorePath(string fileName)
        {
            try
            {
                fileName = NormalizeString(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
                if (_files.ContainsKey(fileName))
                {
                    _files.Remove(fileName);
                    return true;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        public bool SetOrAddFileInfo(string fileName, (DateTime newOffset, long fileLength) fileInfo)
        {
            fileName = NormalizeString(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
            try
            {
                fileName = NormalizeString(fileName); // Remove directory eventually -> don't change casing - Linux has case sensitive file systems
                _files[fileName] = fileInfo;
                return true;
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        public Dictionary<string, (DateTime lastWriteUtc, long fileLength)> GetFileListCopy()
        {
            return new Dictionary<string, (DateTime lastWriteUtc, long fileLength)>(_files);
        }
    }


  public interface IManualScanDirectory
    {
        public FileList Scan(string directory);
    }

    public class ManualScanDirectory : IManualScanDirectory
    {
        public ManualScanDirectory()
        {
        }

        public FileList ScanFastWithFileInfo(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            FileList list = new FileList();
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                try
                {
                    var length = fileInfo.Length;
                    // var lastWriteUtc = fileInfo.LastWriteTimeUtc; Not needed anymore - since not reliable with file links
                    list.Add(file, length);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            return list;
        }

        public FileList Scan(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            FileList list = new FileList();
            foreach (var file in files)
            {
                using var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                try
                {
                    var length = fileStream.Length; // This ensures to get the changed length from the destination file a link is pointing to
                    list.Add(file, length);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            return list;
        }
    }
}
