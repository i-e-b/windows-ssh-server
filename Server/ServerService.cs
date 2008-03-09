using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;

using SshDotNet;
using SshDotNet.Algorithms;

namespace WindowsSshServer
{
    public partial class ServerService : ServiceBase
    {
        //protected delegate void LogClientEventHandler(SshClient client);

        internal const string EventLogName = "Windows-Ssh-Server";
        internal const string EventSourceName = "Windows-Ssh-Server";
        internal const string KeysDirectory = @"../../../Keys/"; // Directory from which to load host keys.

        static ServerService()
        {
            SshTerminalChannel.InjectionDllFileName = Path.Combine(ServerService.GetStartupPath(),
                "ConsoleHook.dll");
        }

        public static new string ServiceName
        {
            get { return "WindowsSshServer"; }
        }

        protected static string GetAssemblyProductName()
        {
            return ((AssemblyProductAttribute)(System.Reflection.Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(AssemblyProductAttribute), false))[0]).Product;
        }

        public static string GetStartupPath()
        {
            //return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        protected SshTcpServer _tcpServer; // TCP server for SSH connections.

        public ServerService()
            : base()
        {
            // Configure event log.
            this.EventLog.Source = EventSourceName;
            this.EventLog.Log = EventLogName;

            // Create TCP server.
            _tcpServer = new SshTcpServer();

            _tcpServer.ClientConnected += new EventHandler<ClientEventArgs>(_tcpServer_ClientConnected);
            _tcpServer.ClientDisconnected += new EventHandler<ClientEventArgs>(
                _tcpServer_ClientDisconnected);

            // Note: need to set in code for Pause to be enabled.
            this.CanPauseAndContinue = true;

            this.Disposed += new EventHandler(SshServerService_Disposed);
        }

        public SshTcpServer TcpServer
        {
            get { return _tcpServer; }
        }

        protected void LogClientAuthEvent(SshClient client, AuthenticationMethod method,
            AuthUserEventArgs authUserEventArgs)
        {
            // Check result of authentication.
            switch (authUserEventArgs.Result)
            {
                case AuthenticationResult.Success:
                    LogClientEvent(client, string.Format("User '{0}' has authenticated using the " +
                        "{1} method.", authUserEventArgs.UserName, method.GetName()),
                        EventLogEntryType.Information);
                    break;
                case AuthenticationResult.FurtherAuthRequired:
                    LogClientEvent(client, string.Format("User '{0}' has authenticated using the " +
                        "{1} method but further authentication is required.", authUserEventArgs.UserName,
                        method.GetName()), EventLogEntryType.Information);
                    break;
                case AuthenticationResult.Failure:
                    LogClientEvent(client, string.Format("User '{0}' has failed to authenticate using the" +
                        "{1} method.", authUserEventArgs.UserName, method.GetName()),
                        EventLogEntryType.Information);
                    break;
                case AuthenticationResult.PasswordExpired:
                    LogClientEvent(client, string.Format("User '{0}' has attempted to authenticate " +
                        "using an expired password.", authUserEventArgs.UserName),
                        EventLogEntryType.Information);
                    break;
                case AuthenticationResult.RequestMoreInfo:
                    LogClientEvent(client, string.Format("User '{0}' has correctly responded to prompts " +
                        "but the server requested more information.", authUserEventArgs.UserName),
                        EventLogEntryType.Information);
                    break;
            }
        }

        protected void LogClientEvent(SshClient client, string message, EventLogEntryType entryType)
        {
            // Write entry to event log.
            var conn = (TcpConnection)client.Connection;

            this.EventLog.WriteEntry(string.Format("Client {0}: {1}", conn.RemoteEndPoint.Address,
                message), entryType);
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


        private void authService_AuthenticationMethodRequested(object sender, AuthMethodRequestedEventArgs e)
        {
            var authService = (SshAuthenticationService)sender;

            // Write to event log.
            LogClientEvent(authService.Client, string.Format("{0} authentication method requested.",
                e.AuthMethod.GetName()), EventLogEntryType.Information);
        }

        private void authService_AuthenticateUserPublicKey(object sender, AuthUserPublicKeyEventArgs e)
        {
            var authService = (SshAuthenticationService)sender;

            e.Result = AuthenticationResult.Success;

            // Write to event log.
            LogClientAuthEvent(authService.Client, AuthenticationMethod.PublicKey, e);
        }

        private void authService_AuthenticateUserPassword(object sender, AuthUserPasswordEventArgs e)
        {
            var authService = (SshAuthenticationService)sender;

            //e.Result = AuthenticationResult.PasswordExpired;

            e.Result = AuthenticationResult.Success;

            // Write to event log.
            LogClientAuthEvent(authService.Client, AuthenticationMethod.Password, e);
        }

        private void authService_AuthenticateUserHostBased(object sender, AuthUserHostBasedEventArgs e)
        {
            var authService = (SshAuthenticationService)sender;

            e.Result = AuthenticationResult.Success;

            // Write to event log.
            LogClientAuthEvent(authService.Client, AuthenticationMethod.HostBased, e);
        }

        private void authService_AuthenticateUserKeyboardInteractive(object sender,
            AuthUserKeyboardInteractiveEventArgs e)
        {
            var authService = (SshAuthenticationService)sender;

            e.Result = AuthenticationResult.Success;

            // Write to event log.
            LogClientAuthEvent(authService.Client, AuthenticationMethod.KeyboardInteractive, e);
        }

        private void authService_ChangePassword(object sender, ChangePasswordEventArgs e)
        {
            var authService = (SshAuthenticationService)sender;

            e.Result = PasswordChangeResult.Failure;

            // Write to event log.
            string resultText = "";

            switch (e.Result)
            {
                case PasswordChangeResult.Success:
                    resultText = "succeeded";
                    break;
                case PasswordChangeResult.FurtherAuthRequired:
                    resultText = "succeeded but further authentication is required";
                    break;
                case PasswordChangeResult.Failure:
                    resultText = "failed";
                    break;
                case PasswordChangeResult.NewPasswordUnacceptable:
                    resultText = "failed because the new password is unacceptable";
                    break;
            }

            LogClientEvent(authService.Client, string.Format("Password change {0}.", resultText),
                EventLogEntryType.Information);
        }

        private void authService_PromptInfoRequested(object sender, PromptInfoRequestedEventArgs e)
        {
            var authService = (SshAuthenticationService)sender;

            e.Name = "Custom Authentication Method";
            e.Instruction = "Enter your password.";
            e.Prompts = new[] { new AuthenticationPrompt("Password: ", true) };

            // Write to event log.
            LogClientEvent(authService.Client, "Prompt info request sent.", EventLogEntryType.Information);
        }

        private void connService_ChannelOpenRequest(object sender, ChannelOpenRequestEventArgs e)
        {
            var channel = new SshTerminalChannel(e);

            e.Channel = channel;
            // e.FailureReason = SshChannelOpenFailureReason.UnknownChannelType;
        }

        private void connService_ChannelOpened(object sender, EventArgs e)
        {
            //
        }

        private void connService_ChannelClosed(object sender, EventArgs e)
        {
            //
        }

        private void Client_KeyExchangeCompleted(object sender, SshKeyExchangeInitializedEventArgs e)
        {
            var client = (SshClient)sender;

            // Load host key for chosen algorithm.
            if (e.HostKeyAlgorithm is SshDss)
            {
                using (var fileStream = new FileStream(Path.Combine(KeysDirectory, @"dss-default.key"),
                    FileMode.Open, FileAccess.Read))
                    e.HostKeyAlgorithm.ImportKey(fileStream);
            }
            else if (e.HostKeyAlgorithm is SshRsa)
            {
                using (var fileStream = new FileStream(Path.Combine(KeysDirectory, @"rsa-default.key"),
                    FileMode.Open, FileAccess.Read))
                    e.HostKeyAlgorithm.ImportKey(fileStream);
            }

            //MessageBox.Show(new SshPublicKey(e.HostKeyAlgorithm).GetFingerprint());

            // Write to event log.
            LogClientEvent(client, "Key exchange completed.", EventLogEntryType.Information);
        }

        private void _tcpServer_ClientConnected(object sender, ClientEventArgs e)
        {
            e.Client.KeyExchangeInitialized += new EventHandler<SshKeyExchangeInitializedEventArgs>(
                Client_KeyExchangeCompleted);

            // Initialize authentication service.
            var authService = e.Client.AuthenticationService;

            authService.BannerMessage = GetAssemblyProductName() + "\r\n";
            authService.AuthenticationMethodRequested += new EventHandler<AuthMethodRequestedEventArgs>(
                authService_AuthenticationMethodRequested);
            authService.AuthenticateUserPublicKey += new EventHandler<AuthUserPublicKeyEventArgs>(
                authService_AuthenticateUserPublicKey);
            authService.AuthenticateUserPassword += new EventHandler<AuthUserPasswordEventArgs>(
                authService_AuthenticateUserPassword);
            authService.AuthenticateUserHostBased += new EventHandler<AuthUserHostBasedEventArgs>(
                authService_AuthenticateUserHostBased);
            authService.AuthenticateUserKeyboardInteractive += new EventHandler<
                AuthUserKeyboardInteractiveEventArgs>(authService_AuthenticateUserKeyboardInteractive);
            authService.ChangePassword += new EventHandler<ChangePasswordEventArgs>(
                authService_ChangePassword);
            authService.PromptInfoRequested += new EventHandler<PromptInfoRequestedEventArgs>(
                authService_PromptInfoRequested);

            // Initialize connection service.
            var connService = e.Client.ConnectionService;

            connService.ChannelOpenRequest += new EventHandler<ChannelOpenRequestEventArgs>(
                connService_ChannelOpenRequest);
            connService.ChannelOpened += new EventHandler<EventArgs>(connService_ChannelOpened);
            connService.ChannelClosed += new EventHandler<EventArgs>(connService_ChannelClosed);

            // Write to event log.
            LogClientEvent(e.Client, "Connected from server.", EventLogEntryType.Information);
        }

        private void _tcpServer_ClientDisconnected(object sender, ClientEventArgs e)
        {
            // Write to event log.
            LogClientEvent(e.Client, "Disconnected from server.", EventLogEntryType.Information);
        }

        private void SshServerService_Disposed(object sender, EventArgs e)
        {
            // Dispose TCP server.
            _tcpServer.Dispose();
        }
    }
}
