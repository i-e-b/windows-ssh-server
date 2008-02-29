using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

using SshDotNet;
using SshDotNet.Algorithms;

namespace WindowsSshServer
{
    public partial class MainForm : Form
    {
        protected const string _keysDir = @"../../../Keys/"; // Directory from which to load host keys.

        protected SshService _service; // Service object for server.

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _service = new SshService();

            // Start server immediately.
            startButton.PerformClick();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _service.Dispose();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Start server, listening for incoming connections.
                _service.TcpServer.Start();
            }
            catch (SocketException exSocket)
            {
                if (exSocket.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    MessageBox.Show(string.Format("The server cannot listen on port {0} because it is " +
                        "already in use by another program.", ((IPEndPoint)exSocket.Data["localEndPoint"])
                        .Port),
                        Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    throw exSocket;
                }
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            // Stop server, closing all open connections.
            _service.TcpServer.Stop();
            _service.TcpServer.CloseAllConnections();
        }

        private void generateKeysButton_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(_keysDir)) Directory.CreateDirectory(_keysDir);

            var dssAlg = new SshDss();
            var rsaAlg = new SshRsa();

            using (var fileStream = new FileStream(Path.Combine(_keysDir, @"dss-default.key"),
                FileMode.Create, FileAccess.Write))
                dssAlg.ExportKey(fileStream);

            using (var fileStream = new FileStream(Path.Combine(_keysDir, @"rsa-default.key"),
                FileMode.Create, FileAccess.Write))
                rsaAlg.ExportKey(fileStream);
        }

        private void updateStatusTimer_Tick(object sender, EventArgs e)
        {
            startButton.Enabled = !_service.TcpServer.IsRunning;
            stopButton.Enabled = _service.TcpServer.IsRunning;

            statusLabel.Text = _service.TcpServer.IsRunning ? "Running" : "Stopped";
            clientCountLabel.Text = string.Format("{0} clients", _service.TcpServer.Clients.Count);
        }
    }
}
