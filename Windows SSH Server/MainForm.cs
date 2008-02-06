using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Reflection;

namespace WindowsSshServer
{
    public partial class MainForm : Form
    {
        protected SshServer _server;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _server = new SshServer();

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

        private void updateStatusTimer_Tick(object sender, EventArgs e)
        {
            startButton.Enabled = !_server.IsRunning;
            stopButton.Enabled = _server.IsRunning;

            statusLabel.Text = _server.IsRunning ? "Running" : "Stopped";
            clientCountLabel.Text = string.Format("{0} clients", _server.Clients.Count);
        }
    }
}
