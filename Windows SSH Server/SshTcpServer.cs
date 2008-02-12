using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WindowsSshServer
{
    public sealed class SshTcpServer : IDisposable
    {
        private TcpListener _tcpListener;     // Listens for TCP connections from clients.
        private List<SshConnection> _clients; // List of connected clients.

        private object _listenerLock;         // Lock for TCP listener.

        private bool _isDisposed = false;     // True if object has been disposed.

        public SshTcpServer()
        {
            _listenerLock = new object();

            _clients = new List<SshConnection>();
        }

        ~SshTcpServer()
        {
            Dispose(false);
        }

        public List<SshConnection> Clients
        {
            get { return _clients; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    Stop();
                    CloseAllConnections();

                    _clients = null;
                }

                // Dispose unmanaged resources.
            }

            _isDisposed = true;
        }

        public bool IsRunning
        {
            get { lock (_listenerLock) return _tcpListener != null; }
        }

        public void Start()
        {
            lock (_listenerLock)
            {
                Start(new IPEndPoint(IPAddress.Any, 22));

                // Begin accepting first incoming connected attempt.
                _tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), _tcpListener);
            }
        }

        public void Start(IPEndPoint localEP)
        {
            lock (_listenerLock)
            {
                // Create TCP listener and start it.
                _tcpListener = new TcpListener(localEP);
                _tcpListener.Start();
            }
        }

        public void Stop()
        {
            lock (_listenerLock)
            {
                if (_tcpListener != null)
                {
                    // Stop TCP listener.
                    _tcpListener.Stop();
                    _tcpListener = null;
                }
            }
        }

        public void CloseAllConnections()
        {
            // Disconnect each client.
            foreach (var client in _clients)
            {
                client.Connected -= client_Connected;
                client.Disconnected -= client_Disconnected;

                client.Dispose();
            }

            // Clear list of clients.
            _clients.Clear();
        }

        private void AcceptTcpClient(IAsyncResult ar)
        {
            // Check that operation has completed.
            if (!ar.IsCompleted) return;

            lock (_listenerLock)
            {
                // Check that operation used current TCP listener.
                if (ar.AsyncState != _tcpListener || _tcpListener == null) return;

                // Accept incoming connection attempt.
                var tcpClient = _tcpListener.EndAcceptTcpClient(ar);

                // Begin accepting next incoming connected attempt.
                _tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), _tcpListener);

                // Add new client to list.
                var sshClient = new SshConsoleClient(new TcpConnection(tcpClient));

                sshClient.Connected += client_Connected;
                sshClient.Disconnected += client_Disconnected;
                sshClient.ConnectionEstablished();

                _clients.Add(sshClient);
            }
        }

        private void client_Connected(object sender, EventArgs e)
        {
            //
        }

        private void client_Disconnected(object sender, EventArgs e)
        {
            SshConnection client = (SshConnection)sender;

            // Remove client from list.
            _clients.Remove(client);
        }
    }
}
