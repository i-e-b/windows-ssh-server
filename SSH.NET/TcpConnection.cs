using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SshDotNet
{
    public class TcpConnection : IConnection
    {
        protected TcpClient _tcpClient;   // Client TCP connection.

        private bool _isDisposed = false; // True if object has been disposed.

        public TcpConnection(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
        }

        ~TcpConnection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                }

                // Dispose unmanaged resources.
            }

            _isDisposed = true;
        }

        public Stream GetStream()
        {
            return _tcpClient.GetStream();
        }

        public void Disconnect(bool remotely)
        {
            // Close network objects.
            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
            }
        }

        public bool HandleException(SshClient client, Exception ex)
        {
            // Check if socket exception was root cause.
            if (ex.InnerException != null && ex.InnerException is SocketException)
            {
                var exSocket = (SocketException)ex.InnerException;

                switch (exSocket.SocketErrorCode)
                {
                    case SocketError.Interrupted:
                        client.Disconnect(false);
                        return true;
                    case SocketError.ConnectionAborted:
                        client.Disconnect(false);
                        return true;
                    case SocketError.ConnectionReset:
                        client.Disconnect(true);
                        return true;
                }
            }

            // Exception was not handled.
            return false;
        }
    }
}
