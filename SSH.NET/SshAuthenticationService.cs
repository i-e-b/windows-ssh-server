using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text;

namespace SshDotNet
{
    public class SshAuthenticationService : SshService
    {
        public event EventHandler<EventArgs> Started;
        public event EventHandler<AuthenticateUserEventArgs> AuthenticateUser;
        public event EventHandler<ChangePasswordEventArgs> ChangePassword;

        protected DateTime _startTime;       // Date/time at which service was started.
        protected Timer _authTimeoutTimer;   // Timer to detect when authentication has timed out.
        protected bool _bannerMsgSent;       // True if banner message has already been sent.
        protected List<AuthenticationMethod>
            _authMethods;                    // List of allowed authentication methods (can change over time).
        protected int _failedAuthAttempts;   // Number of failed authentication attempts made so far.

        private bool _isDisposed = false;    // True if object has been disposed.

        public SshAuthenticationService(SshClient client)
            : base(client)
        {
            _bannerMsgSent = false;
            _authMethods = new List<AuthenticationMethod>() { AuthenticationMethod.PublicKey,
                AuthenticationMethod.Password, AuthenticationMethod.HostBased };
            _failedAuthAttempts = 0;

            // Initialize properties to default values.
            this.Timeout = new TimeSpan(0, 10, 0);
            this.MaximumLoginAttempts = 20;
            this.BannerMessage = null;
            this.BannerMessageLanguage = "";
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        // Dispose managed resources.
                        if (_authTimeoutTimer != null) _authTimeoutTimer.Dispose();
                    }

                    // Dispose unmanaged resources.
                }

                _isDisposed = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public List<AuthenticationMethod> AllowedAuthMethods
        {
            get { return _authMethods; }
        }

        public TimeSpan Timeout
        {
            get;
            set;
        }

        public int MaximumLoginAttempts
        {
            get;
            set;
        }

        public string BannerMessage
        {
            get;
            set;
        }

        public string BannerMessageLanguage
        {
            get;
            set;
        }

        public override string Name
        {
            get { return "ssh-userauth"; }
        }

        internal override void Start()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Set time at which service was started.
            _startTime = DateTime.Now;

            // Create timer to detect timeout.
            _authTimeoutTimer = new Timer(new TimerCallback(AuthTimerCallback), null, 0, 1000);

            // Raise event.
            if (Started != null) Started(this, new EventArgs());
        }

        internal override bool ProcessMessage(byte[] payload)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Check if banner message has not yet been set.
            if (!_bannerMsgSent)
            {
                // Send banner message if one has been specified.
                if (this.BannerMessage != null) SendMsgUserAuthBanner(this.BannerMessage,
                    this.BannerMessageLanguage);

                _bannerMsgSent = true;
            }

            using (var msgStream = new MemoryStream(payload))
            {
                using (var msgReader = new SshStreamReader(msgStream))
                {
                    // Check message ID.
                    SshAuthenticationMessage messageId = (SshAuthenticationMessage)msgReader.ReadByte();

                    switch (messageId)
                    {
                        case SshAuthenticationMessage.Request:
                            ProcessMsgUserAuthRequest(msgReader);
                            break;
                        // Unrecognised message
                        default:
                            return false;
                    }
                }
            }

            // Message was recognised.
            return true;
        }

        protected void AuthTimerCallback(object state)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Check if authentication has timed out.
            if ((DateTime.Now - _startTime) >= this.Timeout)
            {
                // Stop timer.
                _authTimeoutTimer.Dispose();
                _authTimeoutTimer = null;

                // Authentication has timed out.
                _client.Disconnect(false);
            }
        }

        protected void SendMsgUserAuthSuccess()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshAuthenticationMessage.Success);
                }

                // Send User Auth Success message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgUserAuthFailure(IEnumerable<AuthenticationMethod> authsMethods,
            bool partialSuccess)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshAuthenticationMessage.Failure);

                    // Write authentication information.
                    msgWriter.WriteNameList(authsMethods.GetNames());
                    msgWriter.Write(partialSuccess);
                }

                // Send User Auth Failure message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgUserAuthBanner(string message, string language)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshAuthenticationMessage.Banner);

                    // Write banner information.
                    msgWriter.WriteByteString(Encoding.UTF8.GetBytes(message));
                    msgWriter.Write(language);
                }

                // Send User Auth Banner message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgUserAuthPasswdChangeReq(string prompt, string language)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshAuthenticationMessage.PasswordChangeRequired);

                    // Write banner information.
                    msgWriter.WriteByteString(Encoding.UTF8.GetBytes(prompt));
                    msgWriter.Write(language);
                }

                // Send User Auth Password Change Required message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgUserAuthPkOk(string algName, string keyBlob)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshAuthenticationMessage.PublicKeyOk);

                    // Write public key information.
                    msgWriter.Write(algName);
                    msgWriter.Write(keyBlob);
                }

                // Send User Auth Public Key OK message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void ProcessMsgUserAuthRequest(SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Read auth information.
            string userName = Encoding.UTF8.GetString(msgReader.ReadByteString());
            string serviceName = msgReader.ReadString();
            string methodName = msgReader.ReadString();

            // Check if service with specified name exists.
            if (_client.Services.Count(item => item.Name == serviceName) == 0)
            {
                // Service was not found.
                _client.Disconnect(SshDisconnectReason.ServiceNotAvailable, string.Format(
                    "The service with name {0} is not supported by this server."));
                throw new DisconnectedException();
            }

            // Check method of authentication.
            switch (methodName)
            {
                case "publickey":
                    ProcessMsgUserAuthRequestPublicKey(userName, serviceName, msgReader);
                    break;
                case "password":
                    ProcessMsgUserAuthRequestPassword(userName, serviceName, msgReader);
                    break;
                case "hostbased":
                    ProcessMsgUserAuthRequestHostBased(userName, serviceName, msgReader);
                    break;
                case "none":
                    ProcessMsgUserAuthRequestNone(userName, serviceName, msgReader);
                    break;
                default:
                    // Invalid auth method.
                    _client.Disconnect(false);
                    break;
            }
        }

        protected void ProcessMsgUserAuthRequestPublicKey(string userName, string serviceName,
            SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Read request information.
            bool isAuthRequest = msgReader.ReadBoolean();
            string keyAlgName = msgReader.ReadString();
            string keyBlob = msgReader.ReadString();

            if (isAuthRequest)
            {
                // Request is auth request.
                // Read client signature.
                string signature = msgReader.ReadString();

                // Verify signature.
                // See http://www.aota.net/Telnet/puttykeyauth.php4
            }
            else
            {
                // Request is query of whether specified public key is acceptable.
                var keyAlg = _client.PublicKeyAlgorithms.SingleOrDefault(item => item.Name == keyAlgName);

                if (keyAlg != null)
                {
                    // Public key is acceptable.
                    SendMsgUserAuthPkOk(keyAlgName, keyBlob);
                }
                else
                {
                    // Algorithm is not supported.
                    SendMsgUserAuthFailure(this.AllowedAuthMethods, false);
                }
            }
        }

        protected void ProcessMsgUserAuthRequestPassword(string userName, string serviceName,
            SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Check whether client is changing password.
            bool changingPassword = msgReader.ReadBoolean();

            if (changingPassword)
            {
                // Read old and new passwords (in plaintext).
                string oldPassword = Encoding.UTF8.GetString(msgReader.ReadByteString());
                string newPassword = Encoding.UTF8.GetString(msgReader.ReadByteString());

                // Raise event to get result of password change request.
                var changePasswordEventArgs = new ChangePasswordEventArgs(oldPassword, newPassword);

                if (ChangePassword != null) ChangePassword(this, changePasswordEventArgs);

                // Check result of password change request.
                switch (changePasswordEventArgs.Result)
                {
                    case PasswordChangeResult.Success:
                        // Password change has succeeded.
                        SendMsgUserAuthSuccess();

                        // Start requested service.
                        _client.StartService(serviceName);

                        break;
                    case PasswordChangeResult.Failure:
                        // Password change has failed.
                        SendMsgUserAuthFailure(changePasswordEventArgs.AllowedAuthMethods ?? _authMethods,
                            false);

                        break;
                    case PasswordChangeResult.FurtherAuthRequired:
                        // Password change has succeeded, but further auth is required.
                        SendMsgUserAuthFailure(changePasswordEventArgs.AllowedAuthMethods ?? _authMethods,
                            true);

                        break;
                    case PasswordChangeResult.NewPasswordUnacceptable:
                        // Password was not changed.
                        SendMsgUserAuthPasswdChangeReq(changePasswordEventArgs.ReplyPrompt, "");

                        break;
                }
            }
            else
            {
                // Read password (in plaintext).
                string password = Encoding.UTF8.GetString(msgReader.ReadByteString());

                // Raise event to get result of auth attempt.
                var authUserEventArgs = new AuthenticateUserEventArgs(AuthenticationMethod.Password,
                    userName, password);

                if (AuthenticateUser != null) AuthenticateUser(this, authUserEventArgs);

                // Check result of auth attempt.
                switch (authUserEventArgs.Result)
                {
                    case AuthenticationResult.Success:
                        // Auth has succeeded.
                        SendMsgUserAuthSuccess();

                        // Start requested service.
                        _client.StartService(serviceName);

                        break;
                    case AuthenticationResult.Failure:
                        // Increment number of failed auth attempts.
                        _failedAuthAttempts++;

                        if (_failedAuthAttempts < this.MaximumLoginAttempts)
                        {
                            // Auth has failed, but allow client to reattempt auth.
                            SendMsgUserAuthFailure(authUserEventArgs.AllowedAuthMethods ?? _authMethods,
                                false);
                        }
                        else
                        {
                            // Auth has failed too many times, disconnect.
                            _client.Disconnect(false);
                            throw new DisconnectedException();
                        }

                        break;
                    case AuthenticationResult.PasswordExpired:
                        // Password change is required.
                        SendMsgUserAuthPasswdChangeReq("The specified password has expired.", "");

                        break;
                }
            }
        }

        protected void ProcessMsgUserAuthRequestHostBased(string userName, string serviceName,
            SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Read request information.
            string keyAlgName = msgReader.ReadString();
            string keyBlob = msgReader.ReadString();
            string clientHostName = msgReader.ReadString();
            string clientHostUserName = msgReader.ReadString();
            string signature = msgReader.ReadString();

            // Verify signature.
        }

        protected void ProcessMsgUserAuthRequestNone(string userName, string serviceName,
            SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Raise event to get result of auth attempt.
            var authUserEventArgs = new AuthenticateUserEventArgs(AuthenticationMethod.None, userName,
                null);

            if (AuthenticateUser != null) AuthenticateUser(this, authUserEventArgs);

            // Check result of auth attempt.
            switch (authUserEventArgs.Result)
            {
                case AuthenticationResult.Success:
                    // Auth has succeeded.
                    SendMsgUserAuthSuccess();

                    // Start requested service.
                    _client.StartService(serviceName);

                    break;
                case AuthenticationResult.Failure:
                    // Send list of supported auth methods.
                    SendMsgUserAuthFailure(authUserEventArgs.AllowedAuthMethods ?? _authMethods, false);

                    break;
            }
        }
    }

    public static class SshAuthenticationServiceExtensions
    {
        public static string[] GetNames(this IEnumerable<AuthenticationMethod> methods)
        {
            return (from m in methods select m.GetName()).ToArray();
        }

        public static string GetName(this AuthenticationMethod method)
        {
            switch (method)
            {
                case AuthenticationMethod.PublicKey:
                    return "publickey";
                case AuthenticationMethod.Password:
                    return "password";
                case AuthenticationMethod.HostBased:
                    return "hostbased";
                case AuthenticationMethod.None:
                    return "none";
            }

            return "";
        }
    }

    public class AuthenticateUserEventArgs : EventArgs
    {
        public AuthenticateUserEventArgs(AuthenticationMethod authMethod, string userName, string password)
        {
            this.AuthMethod = authMethod;
            this.UserName = userName;
            this.Password = password;
            this.Result = AuthenticationResult.Failure;
            this.AllowedAuthMethods = null;
        }

        public AuthenticationMethod AuthMethod
        {
            get;
            protected set;
        }

        public string UserName
        {
            get;
            protected set;
        }

        public string Password
        {
            get;
            protected set;
        }

        public AuthenticationResult Result
        {
            get;
            set;
        }

        public IEnumerable<AuthenticationMethod> AllowedAuthMethods
        {
            get;
            set;
        }
    }

    public class ChangePasswordEventArgs : EventArgs
    {
        public ChangePasswordEventArgs(string oldPassword, string newPassword)
        {
            this.OldPassword = oldPassword;
            this.NewPassword = newPassword;
            this.Result = PasswordChangeResult.Failure;
            this.AllowedAuthMethods = null;
            this.ReplyPrompt = null;
        }

        public string OldPassword
        {
            get;
            protected set;
        }

        public string NewPassword
        {
            get;
            protected set;
        }

        public PasswordChangeResult Result
        {
            get;
            set;
        }

        public IEnumerable<AuthenticationMethod> AllowedAuthMethods
        {
            get;
            set;
        }

        public string ReplyPrompt
        {
            get;
            set;
        }
    }

    public enum AuthenticationResult
    {
        Success,
        Failure,
        PasswordExpired,
    }

    public enum PasswordChangeResult
    {
        Success,
        Failure,
        FurtherAuthRequired,
        NewPasswordUnacceptable
    }

    public enum AuthenticationMethod
    {
        PublicKey,
        Password,
        HostBased,
        None
    }

    internal enum SshAuthenticationMessage : byte
    {
        Request = 50,
        Failure = 51,
        Success = 52,
        Banner = 53,
        PublicKeyOk = 60,
        PasswordChangeRequired = 60
    }
}
