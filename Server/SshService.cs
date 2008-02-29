using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;

using SshDotNet;
using SshDotNet.Algorithms;

namespace WindowsSshServer
{
    public partial class SshService : ServiceBase
    {
        protected const string _keysDir = @"../../../Keys/"; // Directory from which to load host keys.

        public static new string ServiceName
        {
            get { return "WindowsSshServer"; }
        }

        protected static string GetAssemblyProductName()
        {
            return ((AssemblyProductAttribute)(System.Reflection.Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyProductAttribute), false))[0]).Product;
        }

        protected SshTcpServer _tcpServer; // TCP server for SSH connections.

        public SshService()
            : base()
        {
            // Create TCP server.
            _tcpServer = new SshTcpServer();
            _tcpServer.ClientConnected += new EventHandler<ClientConnectedEventArgs>(
                _tcpServer_ClientConnected);

            // Note: need to set in code for Pause to be enabled.
            this.CanPauseAndContinue = true;
            
            this.Disposed += new EventHandler(SshServerService_Disposed);
        }

        public SshTcpServer TcpServer
        {
            get { return _tcpServer; }
        }

        protected override void OnStart(string[] args)
        {
            // Start TCP server.
            _tcpServer.Start();

            base.OnStart(args);
        }

        protected override void OnStop()
        {
            // Stop TCP server.
            _tcpServer.Stop();

            base.OnStop();
        }

        protected override void OnPause()
        {
            //

            base.OnPause();
        }

        protected override void OnContinue()
        {
            //

            base.OnContinue();
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            //

            return base.OnPowerEvent(powerStatus);
        }

        protected override void OnShutdown()
        {
            //

            base.OnShutdown();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            //

            base.OnSessionChange(changeDescription);
        }

        protected override void OnCustomCommand(int command)
        {
            //

            base.OnCustomCommand(command);
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

        private void authService_AuthenticateUserPublicKey(object sender, AuthenticateUserPublicKeyEventArgs e)
        {
            e.Result = AuthenticationResult.Success;
        }

        private void authService_AuthenticateUserPassword(object sender, AuthenticateUserPasswordEventArgs e)
        {
            e.Result = AuthenticationResult.Success;
            //e.Result = AuthenticationResult.PasswordExpired;
        }

        private void authService_AuthenticateUserHostBased(object sender, AuthenticateUserHostBasedEventArgs e)
        {
            e.Result = AuthenticationResult.Success;
        }

        private void authService_ChangePassword(object sender, ChangePasswordEventArgs e)
        {
            e.Result = PasswordChangeResult.Failure;
        }

        private void authService_RequestPromptInfo(object sender, RequestPromptInfoEventArgs e)
        {
            e.Name = "Custom Auth Method";
            e.Instruction = "Enter your password.";
            e.Prompts = new[] { new AuthenticationPrompt("Password: ", true) };
        }

        private void _tcpServer_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            var authService = e.Client.AuthenticationService;

            e.Client.KeyExchangeInitialized += new EventHandler<SshKeyExchangeInitializedEventArgs>(
                Client_KeyExchangeCompleted);

            authService.BannerMessage = GetAssemblyProductName() + "\r\n";
            authService.AuthenticateUserPublicKey += new EventHandler<AuthenticateUserPublicKeyEventArgs>(
                authService_AuthenticateUserPublicKey);
            authService.AuthenticateUserPassword += new EventHandler<AuthenticateUserPasswordEventArgs>(
                authService_AuthenticateUserPassword);
            authService.AuthenticateUserHostBased += new EventHandler<AuthenticateUserHostBasedEventArgs>(
                authService_AuthenticateUserHostBased);
            authService.ChangePassword += new EventHandler<ChangePasswordEventArgs>(
                authService_ChangePassword);
            authService.RequestPromptInfo += new EventHandler<RequestPromptInfoEventArgs>(
                authService_RequestPromptInfo);
        }

        private void SshServerService_Disposed(object sender, EventArgs e)
        {
            // Dispose TCP server.
            _tcpServer.Dispose();
        }
    }
}
