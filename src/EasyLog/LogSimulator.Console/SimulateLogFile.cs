using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogSimulator
{
    class SimulateLogFile : IDisposable
    {
        private static int LogFileCounter = 0;
        public SimulateLogFile(string directory)
        {
            _fileName = Path.Combine(directory, $"log{Interlocked.Increment(ref LogFileCounter)}");
        }

        public void Start(int delayInMilliseconds = 0)
        {
            _tokenSource?.Cancel();
            _current?.Wait();
            _tokenSource = new CancellationTokenSource();
            _current = Task.Factory.StartNew(() => CreateLogentries(_tokenSource.Token, delayInMilliseconds), TaskCreationOptions.LongRunning);
        }
        public void Stop()
        {
            _tokenSource?.Cancel();
            _current?.Wait();
            _current = null;
            _tokenSource = null;
        }

        private void CreateLogentries(CancellationToken token, int delayInMilliseconds = 0)
        {
            int index = 0;

            string[] streams = { "stdout", "stderr"};
            Random r = new Random();
            FileInfo fi = new FileInfo(_fileName);
            File.Delete(_fileName);
            while (token.IsCancellationRequested == false)
            {
                DateTime time = DateTime.Now;
                string message = $"{++index}  :  {Guid.NewGuid()}";
                string stream = streams[r.Next(0, 2)];
                string logContent = $"{{\"log\":\"{message}\\n\",\"stream\":\"{stream}\",\"time\":\""+
                                    $"{time.Year}-{time.Month}-{time.Day}T{time.ToLongTimeString()}."+
                                    $"{time.Ticks * 100L % 1000000000L + (long)r.Next(0, 10)}Z\"}}\n";
                File.AppendAllText(_fileName, logContent);
                System.Console.WriteLine($"[{index}] Written content to file {_fileName}");
                if (fi.Length > MaxFileSize)
                    File.Delete(_fileName);
                if (delayInMilliseconds > 0)
                    Task.Delay(delayInMilliseconds).Wait();
            }
        }
        
        public void Dispose()
        {
            Stop();
            _fileName = String.Empty;
        }

        const long MaxFileSize = 1024 * 1024 * 10; // Max file size, if larger the file will be deleted
        CancellationTokenSource _tokenSource = null;
        Task _current;
        string _fileName; 
    }
}
