using DirectoryWatcher;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WatcherFileListClasses
{
    public class FileEntry
    {
        public string FileName { get; set; }
        public FileSystemWatcherChangeType LastChanges { get; set; }
    }


    public class ThrottleCalls : IDisposable
    {
        public ThrottleCalls(Action callbackInit, int throttlingInMilliseconds)
        {
            _callback = callbackInit;
            _throttlingInMilliseconds = throttlingInMilliseconds;
        }

        public void Call()
        {
            if (_throttlingInMilliseconds <= 0 || _stopwatch == null || _stopwatch.ElapsedMilliseconds > _throttlingInMilliseconds)
            {
                // Initially calldirectly or if stopwatch is elapsed
                if (_throttlingInMilliseconds > 0)
                    _stopwatch = Stopwatch.StartNew();
                _callback();
                return;
            }


            // Check if semaphore is held -> if so currently a task is running
            if (_sem.WaitAsync(0).Result == false)
            {
                var token = _source.Token;
                Task.Run(async () =>
                {
                    try
                    {
                        int delay = (int)(_throttlingInMilliseconds - _stopwatch.ElapsedMilliseconds);
                        if (delay < 0) delay = 0;
                        await Task.Delay(delay, token);
                    }
                    finally
                    {
                        _stopwatch = Stopwatch.StartNew();
                        _sem.Release();
                        _callback();
                    }
                }, token);
            }
        }

        public void Dispose()
        {
            _source.Cancel();
        }

        private readonly SemaphoreSlim _sem = new SemaphoreSlim(0);
        private readonly CancellationTokenSource _source = new CancellationTokenSource();

        Stopwatch _stopwatch;
        private readonly int _throttlingInMilliseconds;
        private readonly Action _callback;
    }



    public class WatcherFileList : IDisposable
    {
        private readonly FileDirectoryWatcherSettings _fileDirectoryWatcherSettings;
        private readonly object _syncListAccess = new object();
        private readonly IFileSystemWatcher _watcherInterface;
        private readonly int _updateRatioInMilliseconds;
        private Action<ReadOnlyCollection<FileEntry>> _fileListChangeCallback;
        private ThrottleCalls _throttleCalls;
        private List<FileEntry> _currentList;
        private FileDirectoryWatcher _watcher;


        public WatcherFileList(FileDirectoryWatcherSettings settings = null, IFileSystemWatcher watcherInterface = null, int updateRatioInMilliseconds = 0)
        {
            _fileDirectoryWatcherSettings = settings ?? new FileDirectoryWatcherSettings { UseManualScan = true };
            _watcherInterface = watcherInterface;
            _updateRatioInMilliseconds = updateRatioInMilliseconds;
        }

        public void Start(Action<ReadOnlyCollection<FileEntry>> fileListChangeCallback)
        {
            Start(String.Empty, fileListChangeCallback);
        }


        public void Start(string fileFilter, Action<ReadOnlyCollection<FileEntry>> fileListChangeCallback)
        {
            DiscardOldWatcher();
            _watcher = new FileDirectoryWatcher(_fileDirectoryWatcherSettings, _watcherInterface);
            _watcher.Open(new FilterAndCallbackArgument(fileFilter, Callback));
            _fileListChangeCallback = fileListChangeCallback;
            _throttleCalls = new ThrottleCalls(CallAfterChange, _updateRatioInMilliseconds);
        }


        void CallAfterChange()
        {
            ReadOnlyCollection<FileEntry> list;
            lock (_syncListAccess)
            {
                list = _currentList.AsReadOnly();
                _currentList = new List<FileEntry>(); // Event if we overwrite the list it's still held by readonly collection 
            }
            _fileListChangeCallback(list);
        }

        void DiscardOldWatcher()
        {
            lock (_syncListAccess)
            {
                _throttleCalls?.Dispose();
                _watcher?.Dispose();
                _watcher = null;
                _currentList = new List<FileEntry>();
            }
        }

        void Callback(object sender, WatcherCallbackArgs args)
        {
            lock (_syncListAccess)
            {
                bool found = false;
                foreach (var t in _currentList)
                {
                    if (t.FileName == args.FileName)
                    {
                        t.LastChanges |= args.ChangeType;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _currentList.Add(new FileEntry { FileName = args.FileName, LastChanges = args.ChangeType });
                }

                _throttleCalls.Call();
            }

        }

        public void Dispose()
        {
            DiscardOldWatcher();
        }

    }
}
