using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Reflection;

namespace WindowsSshServer
{
    public partial class MainForm : Form
    {
        internal SshTcpServer _server; // TCP server for SSH clients.

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Create TCP server.
            _server = new SshTcpServer();

            // Start server immediately.
            startButton.PerformClick();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _server.Dispose();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            _server.Start();
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            _server.Stop();
            _server.CloseAllConnections();
        }

        private void generateKeysButton_Click(object sender, EventArgs e)
        {
            const string keysDir = @"../../Keys/";

            if (!Directory.Exists(keysDir)) Directory.CreateDirectory(keysDir);

            var dssAlg = new Algorithms.SshDss();
            var rsaAlg = new Algorithms.SshRsa();

            dssAlg.ExportKey(keysDir + @"dss-default.key");
            rsaAlg.ExportKey(keysDir + @"rsa-default.key");
        }

        private void updateStatusTimer_Tick(object sender, EventArgs e)
        {
            startButton.Enabled = !_server.IsRunning;
            stopButton.Enabled = _server.IsRunning;

            statusLabel.Text = _server.IsRunning ? "Running" : "Stopped";
            clientCountLabel.Text = string.Format("{0} clients", _server.Clients.Count);
        }
    }
}
