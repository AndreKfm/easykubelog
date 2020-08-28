using System;
using System.Diagnostics;
using System.IO;

namespace FileToolsClasses
{
    public interface IFileSeeker
    {
        string SeekLastLineFromCurrentAndPositionOnStartOfItAndReturnReadLine(IFileStream stream);
        bool SeekLastLineFromCurrentAndPositionOnStartOfIt(IFileStream stream);
    }

    public class FileSeeker : IFileSeeker
    {
        byte[] _buffer;
        byte[] _crlfBuffer = new byte[1];

        bool SeekNextLineFeedInNegativeDirectionAndPositionStreamOnIt(IFileStream stream, int steps) //, bool skipNearbyCRLF = true)
        {
            if (_buffer == null || (_buffer.Length != steps)) _buffer = new byte[steps];
            Span<byte> buffer = _buffer.AsSpan<byte>();
            var initial = stream.Position;


            for (; ; )
            {
                var current = stream.Position;
                if (current == 0)
                {
                    break;
                }
                int toRead = steps;
                if (toRead > current)
                {
                    toRead = (int)current;
                    buffer = buffer.Slice(0, toRead);
                }
                SetPositionRelative(stream, -toRead);
                var currendMidPos0 = stream.Position;
                int size = stream.Read(buffer);
                var currentMidPos = stream.Position;
                if (size != toRead)
                {
                    // That shouldn't happen ???
                    break;
                }

                int index = buffer.LastIndexOf((byte)'\n');
                if (index >= 0)
                {
                    var posBefore = stream.Position;
                    var newPos = toRead - index;
                    SetPositionRelative(stream, -newPos);
                    var pos = stream.Position;
                    return true;
                }
                SetPositionRelative(stream, -toRead); // Continue with next characters
            }

            SetPosition(stream, initial);
            return false;

        }

        bool SetPositionRelative(IFileStream stream, long offset)
        {
            var current = stream.Position;
            var newPosAbsolute = current + offset;

            var newPos = stream.Seek(offset, SeekOrigin.Current);


            var current2 = stream.Position;
            Debug.Assert((newPos - current) == offset);
            return (newPos - current) == offset; // We assume that we won't position more than Int32.Max
        }

        void SetPosition(IFileStream stream, long position)
        {
            stream.Seek(position, SeekOrigin.Begin);
        }

        public bool SeekLastLineFromCurrentAndPositionOnStartOfIt(IFileStream stream)
        {
            int steps = 80;

            var pos1 = stream.Position;

            var found1 = SeekNextLineFeedInNegativeDirectionAndPositionStreamOnIt(stream, steps);
            if (found1 == false)
            {
                if (pos1 == 0)
                    return false; // We cannot differentiate between String.Empty nothing found and String.Empty = empty log 
                                  // (though by definition right now a log is not empty) but to prevent errors just return null == nothing found
                return false; // No line feed found - so no line yet
            }

            var found2 = SeekNextLineFeedInNegativeDirectionAndPositionStreamOnIt(stream, steps);

            if (found2)
            {
                // Ok we found a second linefeed - so one character after will be the start of our line
                SetPositionRelative(stream, 1);
            }

            // We found one LF but not another one - so there is only one line 
            // -> we can read this line if we position to the begin of the file
            else SetPosition(stream, 0);

            return true;
        }

        public string SeekLastLineFromCurrentAndPositionOnStartOfItAndReturnReadLine(IFileStream stream)
        {
            if (!SeekLastLineFromCurrentAndPositionOnStartOfIt(stream))
                return null;

            var current = stream.Position;

            string result = String.Empty;
            for (; ; )
            {
                int read = stream.Read(_buffer);
                var xxxremove_me_directly = System.Text.Encoding.Default.GetString(_buffer);
                Span<byte> buffer = _buffer.AsSpan<byte>();
                var index = buffer.IndexOf((byte)'\n');
                if (index != -1)
                {

                    // We don't want to have a '\r' at the end of our log line
                    if (index > 0 && buffer[index - 1] == '\r')
                        --index;
                    if (index > 0)
                        result += System.Text.Encoding.Default.GetString(_buffer, 0, index);
                    break;
                }
                if ((read == buffer.Length) && (_buffer[buffer.Length - 1] == '\r'))
                {
                    // Perhaps we haven't found a \n but it could be a \r at the end - if so don't copy \r
                    result += System.Text.Encoding.Default.GetString(_buffer, 0, buffer.Length - 2);
                }
                else
                    result += System.Text.Encoding.Default.GetString(_buffer);
            }
            SetPosition(stream, current); // Reset so we will read the next line backwards on the next call
            if (current == 0 && result == String.Empty)
                return null; // We cannot differentiate between String.Empty nothing found and String.Empty = empty log 
                             // (though by definition right now a log is not empty) but to prevent errors just return null == nothing found
            return result;
        }
    }
}
