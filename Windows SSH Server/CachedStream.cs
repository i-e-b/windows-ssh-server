using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    public class CachedStream : Stream
    {
        protected byte[][] _buffers; // Array of most recent buffers into which stream read.

        protected Stream _stream;    // Base stream, which is to be limited.

        public CachedStream(Stream stream)
            : base()
        {
            _stream = stream;

            _buffers = new byte[2][];
        }

        public Stream BaseStream
        {
            get { return _stream; }
        }

        public override long Position
        {
            get
            {
                return _stream.Position;
            }
            set
            {
                _stream.Position = value;
            }
        }

        public byte[] GetBufferStart(int index, int count)
        {
            byte[] returnBuffer = new byte[count];
            byte[] buffer = GetBuffer(index);

            Buffer.BlockCopy(buffer, 0, returnBuffer, 0, returnBuffer.Length);
            return returnBuffer;
        }

        public byte[] GetBufferEnd(int index, int count)
        {
            byte[] returnBuffer = new byte[count];
            byte[] buffer = GetBuffer(index);

            Buffer.BlockCopy(buffer, buffer.Length - returnBuffer.Length, returnBuffer, 0, 
                returnBuffer.Length);
            return returnBuffer;
        }

        public byte[] GetBuffer(int index)
        {
            return _buffers[index];
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Read bytes from stream.
            int bytesRead = _stream.Read(buffer, offset, count);

            _buffers[0] = _buffers[1];
            _buffers[1] = (byte[])buffer.Clone();

            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }
    }
}
