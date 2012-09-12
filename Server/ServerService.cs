using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        internal const string EventLogName = "Windows-Ssh-Server";
        internal const string EventSourceName = "Windows-Ssh-Server";
        internal const string KeysDirectory = @"../../../Keys/"; // Directory from which to load host keys.

        protected List<SshWinConsoleChannel> _allTermChannels; // List of all terminal channels from all clients.

        static ServerService()
        {
            SshWinConsoleChannel.InjectionDllFileName = Path.Combine(ServerService.GetStartupPath(),
                "ConsoleHook.dll");
        }

        public static new string ServiceName
        {
            get { return "WindowsSshServer"; }
        }

        protected static string GetProductName()
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

            _allTermChannels = new List<SshWinConsoleChannel>();

            // Note: need to set property in code for Pause to be enabled.
            this.CanPauseAndContinue = true;

            this.Disposed += new EventHandler(SshServerService_Disposed);
        }

        public event EventHandler<ChannelListChangedEventArgs> TerminalChannelListChanged;

        public ReadOnlyCollection<SshWinConsoleChannel> AllTerminalChannels
        {
            get { return _allTermChannels.AsReadOnly(); }
        }

        public SshTcpServer TcpServer
        {
            get { return _tcpServer; }
        }

        //public List<SshWinConsoleChannel> GetAllTerminalChannels()
        //{
        //    var list = new List<SshWinConsoleChannel>();

        //    // Add each terminal channel to list.
        //    foreach (var client in _service.TcpServer.Clients)
        //    {
        //        foreach (var channel in client.ConnectionService.Channels)
        //        {
        //            if (channel is SshWinConsoleChannel)
        //                list.Add((SshWinConsoleChannel)channel);
        //        }
        //    }

        //    return list;
        //}

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
            e.Prompts = new[] { new AuthenticationPrompt("Password: ", false) };

            // Write to event log.
            LogClientEvent(authService.Client, "Prompt info request sent.", EventLogEntryType.Information);
        }

        private void connService_ChannelOpenRequest(object sender, ChannelOpenRequestEventArgs e)
        {
            var channel = new SshWinConsoleChannel(e);

            e.Channel = channel;
            // e.FailureReason = SshChannelOpenFailureReason.UnknownChannelType;
        }

        private void connService_ChannelOpened(object sender, ChannelEventArgs e)
        {
            // Check if channel is terminal channel.
            if (e.Channel is SshWinConsoleChannel)
            {
                _allTermChannels.Add((SshWinConsoleChannel)e.Channel);

                // Raise event.
                if (TerminalChannelListChanged != null) TerminalChannelListChanged(this,
                    new ChannelListChangedEventArgs(e.Channel, ChannelListAction.ChannelOpened));
            }
        }

        private void connService_ChannelClosed(object sender, ChannelEventArgs e)
        {
            // Check if channel is terminal channel.
            if (e.Channel is SshWinConsoleChannel)
            {
                _allTermChannels.Remove((SshWinConsoleChannel)e.Channel);

                // Raise event.
                if (TerminalChannelListChanged != null) TerminalChannelListChanged(this,
                    new ChannelListChangedEventArgs(e.Channel, ChannelListAction.ChannelClosed));
            }
        }

        private void connService_ChannelUpdated(object sender, ChannelEventArgs e)
        {
            // Check if channel is terminal channel.
            if (e.Channel is SshWinConsoleChannel)
            {
                // Raise event.
                if (TerminalChannelListChanged != null) TerminalChannelListChanged(this,
                    new ChannelListChangedEventArgs(e.Channel, ChannelListAction.ChannelUpdated));
            }
        }

        private void client_KeyExchangeCompleted(object sender, SshKeyExchangeInitializedEventArgs e)
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
                client_KeyExchangeCompleted);

            // Initialize authentication service.
            var authService = e.Client.AuthenticationService;

            authService.BannerMessage = GetProductName() + "\r\n";
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
            connService.ChannelOpened += new EventHandler<ChannelEventArgs>(connService_ChannelOpened);
            connService.ChannelClosed += new EventHandler<ChannelEventArgs>(connService_ChannelClosed);
            connService.ChannelUpdated += new EventHandler<ChannelEventArgs>(connService_ChannelUpdated);

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

        //protected delegate void LogClientEventHandler(SshClient client);
    }

    public class ChannelListChangedEventArgs : ChannelEventArgs
    {
        public ChannelListChangedEventArgs(SshChannel channel, ChannelListAction action)
            : base(channel)
        {
            this.Action = action;
        }

        public ChannelListAction Action
        {
            get;
            protected set;
        }
    }

    public enum ChannelListAction
    {
        ChannelOpened,
        ChannelClosed,
        ChannelUpdated
    }
}
