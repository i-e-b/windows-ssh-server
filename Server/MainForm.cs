using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            // Load form icon from resource.
            using (var iconStream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(this.GetType(), "Main.ico"))
                this.Icon = new Icon(iconStream);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Position form within screen.
            if (Properties.Settings.Default.MainFormLocation != new Point(-1, -1))
                this.Location = Properties.Settings.Default.MainFormLocation;

            // Create new OS service for server.
            _service = new ServerService();

            _service.TerminalChannelListChanged += new EventHandler<ChannelListChangedEventArgs>(
                _service_TerminalChannelListChanged);
            _service.TcpServer.ClientConnected += new EventHandler<ClientEventArgs>(
                tcpServer_ClientConnected);
            _service.TcpServer.ClientDisconnected += new EventHandler<ClientEventArgs>(
                tcpServer_ClientDisconnected);

            activeSessionsLabel.Text = _service.AllTerminalChannels.Count.ToString();

            // Start server immediately.
            startButton.PerformClick();
        }

        private void connService_ChannelOpened(object sender, ChannelEventArgs e)
        {
            var terminalChannel = e.Channel as SshWinConsoleChannel;

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

        private void _service_TerminalChannelListChanged(object sender, ChannelListChangedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new EventHandler<ChannelListChangedEventArgs>(_service_TerminalChannelListChanged),
                    sender, e);
                return;
            }

            var termChannel = e.Channel as SshWinConsoleChannel;

            // Add/remove item from list box depending on change to list.
            switch (e.Action)
            {
                case ChannelListAction.ChannelOpened:
                    sessionsListBox.Items.Add(termChannel, false);
                    break;
                case ChannelListAction.ChannelClosed:
                    sessionsListBox.Items.Remove(termChannel);
                    break;
                case ChannelListAction.ChannelUpdated:
                    OnTerminalChannelUpdated(termChannel);
                    break;
            }

            activeSessionsLabel.Text = _service.AllTerminalChannels.Count.ToString();
        }

        private void OnTerminalChannelUpdated(SshWinConsoleChannel termChannel)
        {
            // Get index of item corresponding to updated channel in list box.
            var itemIndex = sessionsListBox.Items.IndexOf(termChannel);

            sessionsListBox.SetItemChecked(itemIndex, termChannel.TerminalVisible);
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
            // Create directory for keys if it does not already exist.
            if (!Directory.Exists(_keysDir)) Directory.CreateDirectory(_keysDir);

            // Generate new keys for each algorithm and write them to files.
            var dssAlg = new SshDss();
            var rsaAlg = new SshRsa();

            using (var fileStream = new FileStream(Path.Combine(_keysDir, @"dss-default.key"),
                FileMode.Create, FileAccess.Write))
                dssAlg.ExportKey(fileStream);

            using (var fileStream = new FileStream(Path.Combine(_keysDir, @"rsa-default.key"),
                FileMode.Create, FileAccess.Write))
                rsaAlg.ExportKey(fileStream);

            MessageBox.Show("Cryptographic keys have been successfully regenerated.",
                Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void closeAllTerminalsButton_Click(object sender, EventArgs e)
        {
            if (_service.AllTerminalChannels.Count == 0) return;

            // Confirm action of closing all terminals.
            switch (MessageBox.Show("Are you sure you want to close all active terminal sessions?",
                Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1))
            {
                case DialogResult.Yes:
                    // Close all terminals.
                    foreach (var channel in _service.AllTerminalChannels)
                        channel.Close();

                    break;
            }
        }

        private void showAllTerminalsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            // Show/hide all terminal windows.
            foreach (var channel in _service.AllTerminalChannels)
                channel.TerminalVisible = showAllTerminalsCheckBox.Checked;
        }

        private void sessionsListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Get channel corresponding to item checked.
            var termChannel = sessionsListBox.Items[e.Index] as SshWinConsoleChannel;

            termChannel.TerminalVisible = (e.NewValue == CheckState.Checked);
        }

        private void updateStatusTimer_Tick(object sender, EventArgs e)
        {
            // Enable/disable controls according to server status.
            startButton.Enabled = !_service.TcpServer.IsRunning;
            stopButton.Enabled = _service.TcpServer.IsRunning;

            // Update status text.
            statusLabel.Text = _service.TcpServer.IsRunning ? "Running" : "Stopped";
            clientCountLabel.Text = string.Format("{0} clients", _service.TcpServer.Clients.Count);
        }
    }
}
