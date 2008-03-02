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
        protected const string _eventLogName = "Windows_SSH_Server";
        protected const string _eventSourceName = "Windows_SSH_Server";
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
            // Get path of message resource file.
            var startupPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            var messageFile = Path.Combine(startupPath, "EventLogMsgs.dll");

            // Check if event log source does not yet exist.
            if (!EventLog.SourceExists(_eventSourceName))
            {
                // Create event log.
                var sourceData = new EventSourceCreationData(_eventSourceName, _eventLogName);

                //sourceData.MessageResourceFile = messageFile;
                //sourceData.CategoryResourceFile = messageFile;
                //sourceData.CategoryCount = 0;
                //sourceData.ParameterResourceFile = messageFile;
                
                EventLog.CreateEventSource(sourceData);

                // Register display name for event log.
                this.EventLog.Source = _eventSourceName;
                this.EventLog.Log = _eventLogName;
                this.EventLog.RegisterDisplayName(messageFile, 5001);
            }
            
            // Configure event log.
            this.EventLog.Source = _eventSourceName;
            this.EventLog.Log = _eventLogName;

            this.EventLog.WriteEntry("testing...");

            // Create TCP server.
            _tcpServer = new SshTcpServer();
            _tcpServer.ClientConnected += new EventHandler<ClientConnectedEventArgs>(
                _tcpServer_ClientConnected);

            // Note: need to set in code for Pause to be enabled.
            this.CanPauseAndContinue = true;

            this.Disposed += new EventHandler(SshServerService_Disposed);
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern int GetShortPathName(
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
        string path,
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
        StringBuilder shortPath,
            int shortPathLength
            );

        public string GetShortFileName(string fileName)
        {
            StringBuilder shortPath = new StringBuilder(255);
            GetShortPathName(fileName, shortPath, shortPath.Capacity);
            return shortPath.ToString();
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
            // Stop TCP server.
            _tcpServer.Stop();

            base.OnPause();
        }

        protected override void OnContinue()
        {
            // Start TCP server.
            _tcpServer.Start();

            base.OnContinue();
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            // Save all session/other data?

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

        private void authService_AuthenticateUserPublicKey(object sender, AuthUserPublicKeyEventArgs e)
        {
            e.Result = AuthenticationResult.Success;
        }

        private void authService_AuthenticateUserPassword(object sender, AuthUserPasswordEventArgs e)
        {
            //e.Result = AuthenticationResult.PasswordExpired;

            e.Result = AuthenticationResult.Success;
        }

        private void authService_AuthenticateUserHostBased(object sender, AuthUserHostBasedEventArgs e)
        {
            e.Result = AuthenticationResult.Success;
        }

        private void authService_AuthenticateUserKeyboardInteractive(object sender,
            AuthUserKeyboardInteractiveEventArgs e)
        {
            e.Result = AuthenticationResult.Success;
        }

        private void authService_ChangePassword(object sender, ChangePasswordEventArgs e)
        {
            e.Result = PasswordChangeResult.Failure;
        }

        private void authService_PromptInfoRequested(object sender, PromptInfoRequestedEventArgs e)
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
            authService.AuthenticateUserPublicKey += new EventHandler<AuthUserPublicKeyEventArgs>(
                authService_AuthenticateUserPublicKey);
            authService.AuthenticateUserPassword += new EventHandler<AuthUserPasswordEventArgs>(
                authService_AuthenticateUserPassword);
            authService.AuthenticateUserHostBased += new EventHandler<AuthUserHostBasedEventArgs>(
                authService_AuthenticateUserHostBased);
            authService.AuthenticateUserKeyboardInteractive += new EventHandler<
                AuthUserKeyboardInteractiveEventArgs>(
                authService_AuthenticateUserKeyboardInteractive);
            authService.ChangePassword += new EventHandler<ChangePasswordEventArgs>(
                authService_ChangePassword);
            authService.PromptInfoRequested += new EventHandler<PromptInfoRequestedEventArgs>(
                authService_PromptInfoRequested);
        }

        private void SshServerService_Disposed(object sender, EventArgs e)
        {
            // Dispose TCP server.
            _tcpServer.Dispose();
        }
    }
}
