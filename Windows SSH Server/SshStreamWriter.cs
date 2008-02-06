using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    public class SshStreamWriter : IDisposable
    {
        protected Stream _stream;

        public SshStreamWriter(Stream stream)
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

        public void WriteLine(string value)
        {
            // Write chars of line to stream.
            byte[] buffer = Encoding.ASCII.GetBytes(value + "\r\n");

            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Write(SshMessage messageId)
        {
            Write((byte)messageId);
        }

        public void Write(string[] nameList)
        {
            Write(string.Join(",", nameList));
        }

        public void Write(string value)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(value);

            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Write(char value)
        {
            _stream.WriteByte(Encoding.ASCII.GetBytes(new char[] { value })[0]);
        }

        public void Write(ushort value)
        {
            byte[] buffer = new byte[2];

            buffer[0] = (byte)((value & 0xFF00) >> 8);
            buffer[1] = (byte)(value & 0x00FF);

            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Write(uint value)
        {
            byte[] buffer = new byte[4];

            buffer[0] = (byte)((value & 0xFF000000) >> 24);
            buffer[1] = (byte)((value & 0x00FF0000) >> 16);
            buffer[2] = (byte)((value & 0x0000FF00) >> 8);
            buffer[3] = (byte)(value & 0x000000FF);

            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Write(ulong value)
        {
            byte[] buffer = new byte[8];

            buffer[0] = (byte)((value & 0xFF00000000000000) >> 56);
            buffer[1] = (byte)((value & 0x00FF000000000000) >> 48);
            buffer[2] = (byte)((value & 0x0000FF0000000000) >> 40);
            buffer[3] = (byte)((value & 0x000000FF00000000) >> 32);
            buffer[4] = (byte)((value & 0x00000000FF000000) >> 24);
            buffer[5] = (byte)((value & 0x0000000000FF0000) >> 16);
            buffer[6] = (byte)((value & 0x000000000000FF00) >> 8);
            buffer[7] = (byte)(value & 0x00000000000000FF);

            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Write(bool value)
        {
            _stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        public void Write(byte value)
        {
            _stream.WriteByte(value);
        }

        public void Write(byte[] buffer)
        {
            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public virtual void Close()
        {
            this.Dispose(true);
        }
    }
}
