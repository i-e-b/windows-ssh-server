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
        protected SshTcpServer _server; // TCP server for SSH clients.

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Create TCP server.
            _server = new SshTcpServer();

            _server.ClientConnected += new EventHandler<ClientConnectedEventArgs>(_server_ClientConnected);

            // Start server immediately.
            startButton.PerformClick();
        }

        private void _server_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            var authService = e.Client.AuthenticationService;

            authService.BannerMessage = Application.ProductName + "\r\n";
            authService.AuthenticateUser += new EventHandler<AuthenticateUserEventArgs>(
                authService_AuthenticateUser);
            authService.ChangePassword += new EventHandler<ChangePasswordEventArgs>(
                authService_ChangePassword);
        }

        private void authService_AuthenticateUser(object sender, AuthenticateUserEventArgs e)
        {
            if (e.AuthMethod == AuthenticationMethod.None) return;

            e.Result = AuthenticationResult.PasswordExpired;
        }

        private void authService_ChangePassword(object sender, ChangePasswordEventArgs e)
        {
            e.Result = PasswordChangeResult.Failure;
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

            if (_server.Clients.Count > 0)
            {
                this.Text = _server.Clients[0].BytesTransmittedSinceLastKex.ToString();
            }
        }
    }
}
