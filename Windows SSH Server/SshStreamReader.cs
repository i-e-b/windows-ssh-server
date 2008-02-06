using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    public class SshStreamReader : IDisposable
    {
        protected Stream _stream;

        public SshStreamReader(Stream stream)
        {
            _stream = stream;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stream stream = _stream;
                _stream = null;
                if (stream != null) stream.Close();
            }

            _stream = null;
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
        }

        public Stream BaseStream
        {
            get { return _stream; }
        }

        public string ReadLine()
        {
            var lineBuilder = new StringBuilder();

            // Read chars from stream until LF (line feed) is found.
            char curChar;

            do
            {
                curChar = ReadChar();
                lineBuilder.Append(curChar);
            } while (curChar != '\n');

            // Return line without trailing CR LF.
            return lineBuilder.ToString(0, lineBuilder.Length - 2);
        }

        public SshMessage ReadMessageId()
        {
            return (SshMessage)ReadByte();
        }

        public string[] ReadNameList()
        {
            return ReadString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string ReadString()
        {
            // Read string of known length from stream.
            uint length = ReadUInt32();
            byte[] buffer = new byte[length];

            int bytesRead = _stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0 && buffer.Length > 0) throw new EndOfStreamException();

            return Encoding.ASCII.GetString(buffer, 0, bytesRead);
        }

        public char ReadChar()
        {
            int num = _stream.ReadByte();

            if (num == -1) throw new EndOfStreamException();
            return Encoding.ASCII.GetChars(new byte[] { (byte)num })[0];
        }

        public ushort ReadUInt16()
        {
            return (ushort)((ReadByte() << 8) | ReadByte());
        }

        public uint ReadUInt32()
        {
            return (uint)((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
        }

        public ulong ReadUInt64()
        {
            return (ulong)((ReadByte() << 56) | (ReadByte() << 48) | (ReadByte() << 40) | (ReadByte() << 32) |
                (ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
        }

        public bool ReadBoolean()
        {
            int num = _stream.ReadByte();

            if (num == -1) throw new EndOfStreamException();
            return (num != 0);
        }

        public byte[] ReadBytes(int count)
        {
            byte[] buffer = new byte[count];

            int bytesRead = _stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0 && buffer.Length > 0) throw new EndOfStreamException();

            byte[] returnBuffer = new byte[bytesRead];
            Buffer.BlockCopy(buffer, 0, returnBuffer, 0, returnBuffer.Length);

            return returnBuffer;
        }

        public byte ReadByte()
        {
            int num = _stream.ReadByte();

            if (num == -1) throw new EndOfStreamException();
            return (byte)num;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public virtual void Close()
        {
            this.Dispose(true);
        }
    }
}
