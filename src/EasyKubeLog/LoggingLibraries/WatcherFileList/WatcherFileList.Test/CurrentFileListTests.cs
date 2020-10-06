using System;
using System.Text;



namespace WatcherFileListClasses.Test
{
    using FileToolsClasses;
    using Moq;
    using System.IO;
    using Xunit;
    public class CurrentFileListTests
    {
        [Fact]
        public void AddFile()
        {
            Mock<IFile> m = new Mock<IFile>();
            WatcherCurrentFileList c = new WatcherCurrentFileList();
            c.AddFile(new CurrentFileEntry("test1.txt", m.Object));
            var l = c.GetList();
            Assert.True(l.Count == 1);
            Assert.True(l.ContainsKey("test1.txt"));
        }

        [Fact]
        public void RemoveFile()
        {
            Mock<IFile> m = new Mock<IFile>();
            WatcherCurrentFileList c = new WatcherCurrentFileList();
            Assert.False(c.RemoveFile("test1.txt"));
            Assert.True(c.AddFile(new CurrentFileEntry("test1.txt", m.Object)));
            var l = c.GetList();
            Assert.True(l.Count == 1);
            Assert.True(l.ContainsKey("test1.txt"));
            Assert.True(c.RemoveFile("test1.txt"));
            Assert.True(c.GetList().Count == 0);
        }
    }

    public class FileReadOnlyWrapperTests
    {
        [Fact]
        public void CreateDispose()
        {
            FileReadOnlyWrapper w = new FileReadOnlyWrapper("dummy.txt");
            w.Dispose();
        }

        [Fact]
        public void CreateDispose_MockedFileStream()
        {
            var m = new Mock<IFileStream>();
            FileReadOnlyWrapper w = new FileReadOnlyWrapper("dummy.txt", m.Object);
            w.Dispose();
        }


        public class FileStreamHelper
        {
            private readonly Mock<IFileStream> _m;

            public IFileStream Object()
            {
                return _m.Object;
            }

            public FileStreamHelper()
            {
                ReturnString = String.Empty;
                _m = new Mock<IFileStream>();
                _m.SetupGet(x => x.Position).Returns(() => _position);



                _m.SetupGet(x => x.Length).Returns(() => _fileSize);
                _m.Setup(x => x.Seek(It.IsAny<long>(), It.IsAny<SeekOrigin>())).Callback((long pos, SeekOrigin origin) =>
                {
                    _position = Seek(pos, origin, ref _position, _fileSize);
                }).Returns(() => _position);



                _m.Setup(x => x.Read(It.IsAny<byte[]>())).Callback((byte[] buffer) =>
                {
                    _bytesRead = Read(buffer);
                    //position = position + bytesRead;
                }).Returns(() => _bytesRead);

            }

            long Seek(long pos, SeekOrigin origin, ref long position, long fileSize)
            {
                if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        {
                            position = pos;
                            break;
                        }
                    case SeekOrigin.End:
                        {
                            position = fileSize - pos;
                            break;
                        }
                    case SeekOrigin.Current:
                        {
                            position += pos;
                            break;
                        }
                }
                return position;
            }


            private string _returnString;
            public string ReturnString
            {
                get => _returnString;
                set { _returnString = value; if (_fileSize < _returnString.Length) _fileSize = _returnString.Length; }
            }

            private int _bytesRead;
            private long _fileSize = 1024;
            private long _position;
            int Read(byte[] buffer)
            {
                var bytes = Encoding.UTF8.GetBytes(ReturnString);
                //if (buffer.Length < bytes.Length)
                //    return 0;

                if (bytes.Length < (buffer.Length + _position))
                    return 0;

                for (int i = 0; i < buffer.Length; ++i)
                {
                    buffer[i] = bytes[i + _position];
                }

                _bytesRead = buffer.Length;
                _position += _bytesRead;
                return _bytesRead;
            }

            public void WriteReadCheck(string input, string expected, int maxSize = 16384)
            {
                var fileStream = Object();
                FileReadOnlyWrapper w = new FileReadOnlyWrapper("dummy.txt", fileStream);
                ReturnString = input;
                var read = w.FOR_UNIT_TEST_SLOW_FUNCTION_ReadLineCharByCharTillCRLF();
                Assert.Equal(expected, read);
                fileStream.Seek(0, SeekOrigin.Begin); // Reset
            }

        }


        [Fact]
        public void ReadContent_BasicReads()
        {
            FileStreamHelper f = new FileStreamHelper();

            f.Object();

            string input = "hello world\r\n";
            f.WriteReadCheck(input, input);

            f.WriteReadCheck("\r\n", "\r\n");

        }

        [Fact]
        public void ReadContent_BasicReads_Umlaute()
        {
            FileStreamHelper f = new FileStreamHelper();

            f.Object();

            // ReSharper disable once StringLiteralTypo
            string input = "hello worldöäüÖÄÜß你好，世界\r\n";
            f.WriteReadCheck(input, input);
            f.WriteReadCheck("\r\n", "\r\n");
            f.WriteReadCheck("\n", "\n");
        }

        [Fact]
        public void ReadContent_UnfinishedLine()
        {
            FileStreamHelper f = new FileStreamHelper();

            f.WriteReadCheck("hello world\r", String.Empty);
            f.WriteReadCheck("hello world\r\n", "hello world\r\n");
            f.WriteReadCheck("", String.Empty);
            f.WriteReadCheck("              ", String.Empty);
            f.WriteReadCheck("", String.Empty);

        }

        [Fact]
        public void ReadContent_Read2Lines()
        {
            FileStreamHelper f = new FileStreamHelper();

            f.Object();

            string returnStringX = "h1\r\n";
            string input = "\r\nh1\r\nhello world\r\n";
            f.WriteReadCheck(input, "\r\n");

            input = "h1\r\nhello world";
            f.WriteReadCheck(input, returnStringX);
        }

        [Fact]
        public void ReadContent_BigContent()
        {
            FileStreamHelper f = new FileStreamHelper();

            f.Object();

            string bigInput = new String((char)65, 65536);
            bigInput += Environment.NewLine;
            f.WriteReadCheck(bigInput, bigInput, bigInput.Length);
        }


    }
}
