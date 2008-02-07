using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WindowsSshServer
{
    internal sealed class SshServer : IDisposable
    {
        private TcpListener _tcpListener; // Listens for TCP connections from clients.
        private List<SshClient> _clients; // List of connected clients.

        private bool _isDisposed = false; // True if object has been disposed.

        public SshServer()
        {
            _clients = new List<SshClient>();
        }

        ~SshServer()
        {
            Dispose(false);
        }

        public List<SshClient> Clients
        {
            get { return _clients; }
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsRunning
        {
            get { return _tcpListener != null; }
        }

        public void Start()
        {
            Start(new IPEndPoint(IPAddress.Any, 22));

            // Begin accepting first incoming connected attempt.
            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), _tcpListener);
        }

        public void Start(IPEndPoint localEP)
        {
            _tcpListener = new TcpListener(localEP);
            _tcpListener.Start();
        }

        public void Stop()
        {
            if (_tcpListener != null)
            {
                _tcpListener.Stop();
                _tcpListener = null;

                GC.Collect();
            }
        }

        public void CloseAllConnections()
        {
            // Disconnect each client.
            foreach (var client in _clients)
            {
                client.Connected -= client_Connected;
                client.Disconnected -= client_Disconnected;

                client.Disconnect();
            }

            // Clear list of clients.
            _clients.Clear();
        }

        private void AcceptTcpClient(IAsyncResult ar)
        {
            // Check that operation has completed.
            if (!ar.IsCompleted) return;

            // Check that operation used current TCP listener.
            if (ar.AsyncState != _tcpListener || _tcpListener == null) return;

            // Accept incoming connection attempt.
            TcpClient tcpClient;

            try
            {
                 tcpClient = _tcpListener.EndAcceptTcpClient(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            // Begin accepting next incoming connected attempt.
            _tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), _tcpListener);

            // Add new client to list.
            var sshClient = new SshClient(tcpClient);

            sshClient.Connected += client_Connected;
            sshClient.Disconnected += client_Disconnected;

            _clients.Add(sshClient);
        }

        private void client_Connected(object sender, EventArgs e)
        {
            //
        }

        private void client_Disconnected(object sender, EventArgs e)
        {
            SshClient client = (SshClient)sender;

            // Remove client from list.
            _clients.Remove(client);
        }
    }
}
