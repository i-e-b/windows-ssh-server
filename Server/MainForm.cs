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

        protected ServerService _service; // Service object for server.

        public MainForm()
        {
            InitializeComponent();
        }

        protected List<SshTerminalChannel> GetAllTerminalChannels()
        {
            var list = new List<SshTerminalChannel>();

            // Add each terminal channel to list.
            foreach (var client in _service.TcpServer.Clients)
            {
                foreach (var channel in client.ConnectionService.Channels)
                {
                    if (channel is SshTerminalChannel)
                        list.Add((SshTerminalChannel)channel);
                }
            }

            return list;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Position form within screen.
            if (Properties.Settings.Default.MainFormLocation != new Point(-1, -1))
                this.Location = Properties.Settings.Default.MainFormLocation;

            // Create service for server.
            _service = new ServerService();

            _service.TcpServer.ClientConnected += new EventHandler<ClientEventArgs>(
                tcpServer_ClientConnected);
            _service.TcpServer.ClientDisconnected += new EventHandler<ClientEventArgs>(
                tcpServer_ClientDisconnected);

            // Start server immediately.
            startButton.PerformClick();
        }

        private void connService_ChannelOpened(object sender, ChannelEventArgs e)
        {
            var terminalChannel = e.Channel as SshTerminalChannel;

            terminalChannel.TerminalVisible = showAllTerminalsCheckBox.Checked;
        }

        private void tcpServer_ClientConnected(object sender, ClientEventArgs e)
        {
            e.Client.ConnectionService.ChannelOpened += new EventHandler<ChannelEventArgs>(
                connService_ChannelOpened);
        }

        private void tcpServer_ClientDisconnected(object sender, ClientEventArgs e)
        {
            //
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Properties.Settings.Default.MainFormLocation = this.Location;

            // Dispose service.
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

        private void closeAllTerminalsButton_Click(object sender, EventArgs e)
        {
            // Close all terminals.
            var terminalChannels = GetAllTerminalChannels();

            terminalChannels.ForEach(channel => channel.Close());
        }

        private void showAllTerminalsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // Show/hide all terminals.
            var terminalChannels = GetAllTerminalChannels();

            terminalChannels.ForEach(channel => channel.TerminalVisible
                = showAllTerminalsCheckBox.Checked);
        }

        private void updateStatusTimer_Tick(object sender, EventArgs e)
        {
            // Enable/disable controls.
            startButton.Enabled = !_service.TcpServer.IsRunning;
            stopButton.Enabled = _service.TcpServer.IsRunning;

            // Update status text.
            statusLabel.Text = _service.TcpServer.IsRunning ? "Running" : "Stopped";
            clientCountLabel.Text = string.Format("{0} clients", _service.TcpServer.Clients.Count);

            activeSessionsLabel.Text = GetAllTerminalChannels().Count.ToString();
        }
    }
}
