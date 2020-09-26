using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogSimulator
{
    class SimulateLogFile : IDisposable
    {
        private static int _logFileCounter;
        public SimulateLogFile(string directory)
        {
            _fileName = Path.Combine(directory, $"log{Interlocked.Increment(ref _logFileCounter)}");
        }

        public void Start(int delayInMilliseconds = 0)
        {
            _tokenSource?.Cancel();
            _current?.Wait();
            _tokenSource = new CancellationTokenSource();
            _current = Task.Factory.StartNew(() => CreateLogEntries(_tokenSource.Token, delayInMilliseconds), TaskCreationOptions.LongRunning);
        }
        public void Stop()
        {
            _tokenSource?.Cancel();
            _current?.Wait();
            _current = null;
            _tokenSource = null;
        }

        protected readonly Random _rand = new Random();
        private string CreateRandomString(int len)
        {
            var builder = new StringBuilder(len);
            for (int i = 0; i < len; ++i)
            {
                builder.Append((char)_rand.Next('A', 'Z'));
            }
            return builder.ToString();
        }

        private void CreateLogEntries(CancellationToken token, int delayInMilliseconds = 0)
        {
            int secondOffset = 0;
            int index = 0;

            string[] streams = { "stdout", "stderr" };
            Random r = new Random();
            FileInfo fi = new FileInfo(_fileName);
            File.Delete(_fileName);

            Stopwatch w = Stopwatch.StartNew();
            long data = 0;
            while (token.IsCancellationRequested == false)
            {
                secondOffset++;
                DateTime time = DateTime.Now;
                time += TimeSpan.FromSeconds(secondOffset);
                string message = $"{++index}  :  {Guid.NewGuid()}  {CreateRandomString(40)}";
                string stream = streams[r.Next(0, 2)];
                string logContent = $"{{\"log\":\"{message}\\n\",\"stream\":\"{stream}\",\"time\":\"" +
                                    $"{time.Year}-{time.Month}-{time.Day}T{time.ToLongTimeString()}." +
                                    $"{time.Ticks * 100L % 1000000000L }Z\"}}\n";
                File.AppendAllText(_fileName, logContent);

                data += logContent.Length;

                if (w.ElapsedMilliseconds > 2000)
                {
                    var needed = w.ElapsedMilliseconds;
                    w.Restart();
                    Console.WriteLine($"[{index}] Written content to file {_fileName} {logContent}");
                    Console.WriteLine($"# Bytes / second { (1000.0 * (double)data) / needed}");
                    data = 0;
                }
                if (fi.Length > MaxFileSize)
                    File.Delete(_fileName);
                if (delayInMilliseconds > 0)
                    Task.Delay(delayInMilliseconds, token).Wait(token);
            }
        }

        public void Dispose()
        {
            Stop();
            _fileName = String.Empty;
        }

        const long MaxFileSize = 1024 * 1024 * 10; // Max file size, if larger the file will be deleted
        CancellationTokenSource _tokenSource;
        Task _current;
        string _fileName;
    }
}
