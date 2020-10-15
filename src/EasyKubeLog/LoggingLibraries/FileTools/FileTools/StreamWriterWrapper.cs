using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FileToolsClasses
{
    public interface IStreamWriterWrapper : IDisposable
    {
        public Task Write(string textLine);
        public void Flush();
    }

    public class StreamWriterWrapperImpl : IStreamWriterWrapper
    {
        private readonly StreamWriter _writer;

        public StreamWriterWrapperImpl(FileStream file)
        {
            _writer = new StreamWriter(file);
        }

        public async Task Write(string textLine)
        {
            await _writer.WriteAsync(textLine);
        }

        public void Flush()
        {
            _writer.Flush();
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }
    }
}
