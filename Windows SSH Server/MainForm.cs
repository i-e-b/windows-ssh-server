using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
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

            e.Client.KeyExchangeInitialized += new EventHandler<SshKeyExchangeInitializedEventArgs>(Client_KeyExchangeCompleted);

            authService.BannerMessage = Application.ProductName + "\r\n";
            authService.AuthenticateUser += new EventHandler<AuthenticateUserEventArgs>(
                authService_AuthenticateUser);
            authService.ChangePassword += new EventHandler<ChangePasswordEventArgs>(
                authService_ChangePassword);
        }

        private void Client_KeyExchangeCompleted(object sender, SshKeyExchangeInitializedEventArgs e)
        {
            // Load host key for chosen algorithm.
            if (e.HostKeyAlgorithm is SshDss)
            {
                using (var fileStream = new FileStream(Path.Combine(_keysDir, @"dss-default.key"),
                    FileMode.Open, FileAccess.Read))
                    e.HostKeyAlgorithm.ImportKey(fileStream);
            }
            else if (e.HostKeyAlgorithm is SshRsa)
            {
                using (var fileStream = new FileStream(Path.Combine(_keysDir, @"rsa-default.key"),
                    FileMode.Open, FileAccess.Read))
                    e.HostKeyAlgorithm.ImportKey(fileStream);
            }

            //MessageBox.Show(new SshPublicKey(e.HostKeyAlgorithm).GetFingerprint());
        }

        private void authService_AuthenticateUser(object sender, AuthenticateUserEventArgs e)
        {
            if (e.AuthMethod == AuthenticationMethod.None) return;

            e.Result = AuthenticationResult.Success;
            //e.Result = AuthenticationResult.PasswordExpired;
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
            startButton.Enabled = !_server.IsRunning;
            stopButton.Enabled = _server.IsRunning;

            statusLabel.Text = _server.IsRunning ? "Running" : "Stopped";
            clientCountLabel.Text = string.Format("{0} clients", _server.Clients.Count);
        }
    }
}
