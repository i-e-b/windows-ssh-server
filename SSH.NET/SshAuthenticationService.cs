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
        public event EventHandler<AuthenticateUserNoMethodEventArgs> AuthenticateUserNoMethod;
        public event EventHandler<AuthenticateUserPublicKeyEventArgs> AuthenticateUserPublicKey;
        public event EventHandler<AuthenticateUserPasswordEventArgs> AuthenticateUserPassword;
        public event EventHandler<AuthenticateUserHostBasedEventArgs> AuthenticateUserHostBased;
        public event EventHandler<ChangePasswordEventArgs> ChangePassword;
        public event EventHandler<EventArgs> UserAuthenticated;

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
                AuthenticationMethod.Password, AuthenticationMethod.HostBased, 
                AuthenticationMethod.KeyboardInteractive };
            _failedAuthAttempts = 0;

            // Initialize properties to default values.
            this.Timeout = new TimeSpan(0, 10, 0);
            this.MaximumAuthAttempts = 20;
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

        public int MaximumAuthAttempts
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

            base.Start();
        }

        internal override void Stop()
        {
            base.Stop();
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
                        // User auth messages
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
            if (_authTimeoutTimer != null && (DateTime.Now - _startTime) >= this.Timeout)
            {
                // Stop timer.
                _authTimeoutTimer.Dispose();
                _authTimeoutTimer = null;

                // Authentication has timed out.
                _client.Disconnect(false);
            }
        }

        protected void SendMsgUserAuthInfoResponse()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshAuthenticationMessage.InfoResponse);

                    //
                }

                // Send User Auth Info Response message.
                _client.SendPacket(msgStream.ToArray());
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

        protected void SendMsgUserAuthFailure(bool partialSuccess)
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
                    msgWriter.WriteNameList(this.AllowedAuthMethods.GetNames());
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

        protected void SendMsgUserAuthPkOk(string algName, byte[] keyBlob)
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
                    msgWriter.WriteByteString(keyBlob);
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
                case "none":
                    ProcessMsgUserAuthRequestNone(userName, serviceName, msgReader);
                    break;
                case "publickey":
                    ProcessMsgUserAuthRequestPublicKey(userName, serviceName, msgReader);
                    break;
                case "password":
                    ProcessMsgUserAuthRequestPassword(userName, serviceName, msgReader);
                    break;
                case "hostbased":
                    ProcessMsgUserAuthRequestHostBased(userName, serviceName, msgReader);
                    break;
                case "keyboard-interactive":
                    ProcessMsgUserAuthRequestKeyboardInteractive(userName, serviceName, msgReader);
                    break;
                default:
                    // Invalid auth method.
                    _client.Disconnect(false);
                    break;
            }
        }

        protected void ProcessMsgUserAuthRequestNone(string userName, string serviceName,
            SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Raise event to get result of auth attempt.
            var authUserEventArgs = new AuthenticateUserNoMethodEventArgs(userName);

            if (AuthenticateUserNoMethod != null) AuthenticateUserNoMethod(this, authUserEventArgs);

            // Check result of auth attempt.
            switch (authUserEventArgs.Result)
            {
                case AuthenticationResult.Success:
                    // Auth has succeeded.
                    AuthenticateUser(serviceName);

                    break;
                case AuthenticationResult.Failure:
                    // Send list of supported auth methods.
                    SendMsgUserAuthFailure(false);

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
            byte[] keyAndCertsData = msgReader.ReadByteString();

            // Try to find public key algorithm.
            PublicKeyAlgorithm keyAlg = null;

            try
            {
                keyAlg = (PublicKeyAlgorithm)_client.PublicKeyAlgorithms.Single(item =>
                    item.Name == keyAlgName).Clone();
            }
            catch (InvalidOperationException)
            {
                // Public key algorithm is not supported.
                SendMsgUserAuthFailure(false);
            }

            // Load key and certificats data for algorithm.
            keyAlg.LoadKeyAndCertificatesData(keyAndCertsData);

            // Check if request is actual auth request or query of whether specified public key is
            // acceptable.
            if (isAuthRequest)
            {
                // Read client signature.
                var signatureData = msgReader.ReadByteString();
                var signature = keyAlg.GetSignature(signatureData);

                // Verify signature.
                var payloadData = ((MemoryStream)msgReader.BaseStream).ToArray();

                if (VerifyPublicKeySignature(keyAlg, payloadData, 0, payloadData.Length -
                   signatureData.Length - 4, signature))
                {
                    // Raise event to get result of auth attempt.
                    var authUserEventArgs = new AuthenticateUserPublicKeyEventArgs(userName,
                        keyAlg.ExportPublicKey());

                    AuthenticateUserPublicKey(this, authUserEventArgs);

                    // Check result of auth attempt.
                    switch (authUserEventArgs.Result)
                    {
                        case AuthenticationResult.Success:
                            // Auth has succeeded.
                            AuthenticateUser(serviceName);

                            break;
                        case AuthenticationResult.FurtherAuthRequired:
                            // Auth has succeeded, but further auth is required.
                            SendMsgUserAuthFailure(true);

                            break;
                        case AuthenticationResult.Failure:
                            // Auth has failed.
                            SendMsgUserAuthFailure(false);

                            break;
                    }
                }
                else
                {
                    // Signature is invalid.
                    SendMsgUserAuthFailure(false);
                }
            }
            else
            {
                // Public key is acceptable.
                SendMsgUserAuthPkOk(keyAlgName, keyAndCertsData);
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
                        // Password change and auth have succeeded.
                        AuthenticateUser(serviceName);

                        break;
                    case PasswordChangeResult.FurtherAuthRequired:
                        // Password change has succeeded, but further auth is required.
                        SendMsgUserAuthFailure(true);

                        break;
                    case PasswordChangeResult.Failure:
                        // Password change has failed.
                        SendMsgUserAuthFailure(false);

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
                var authUserEventArgs = new AuthenticateUserPasswordEventArgs(userName, password);

                if (AuthenticateUserPassword != null) AuthenticateUserPassword(this, authUserEventArgs);

                // Check result of auth attempt.
                switch (authUserEventArgs.Result)
                {
                    case AuthenticationResult.Success:
                        // Auth has succeeded.
                        AuthenticateUser(serviceName);

                        break;
                    case AuthenticationResult.FurtherAuthRequired:
                        // Auth has succeeded, but further auth is required.
                        SendMsgUserAuthFailure(true);

                        break;
                    case AuthenticationResult.Failure:
                        // Increment number of failed auth attempts.
                        _failedAuthAttempts++;

                        if (_failedAuthAttempts < this.MaximumAuthAttempts)
                        {
                            // Auth has failed, but allow client to reattempt auth.
                            SendMsgUserAuthFailure(false);
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
            byte[] keyAndCertsData = msgReader.ReadByteString();
            string clientHostName = msgReader.ReadString();
            string clientUserName = msgReader.ReadString();

            // Try to find public key algorithm.
            PublicKeyAlgorithm keyAlg = null;

            try
            {
                keyAlg = (PublicKeyAlgorithm)_client.PublicKeyAlgorithms.Single(item =>
                    item.Name == keyAlgName).Clone();
            }
            catch (InvalidOperationException)
            {
                // Public key algorithm is not supported.
                SendMsgUserAuthFailure(false);
            }

            // Load key and certificats data for algorithm.
            keyAlg.LoadKeyAndCertificatesData(keyAndCertsData);

            // Read client signature.
            var signatureData = msgReader.ReadByteString();
            var signature = keyAlg.GetSignature(signatureData);

            // Verify signature.
            var payloadData = ((MemoryStream)msgReader.BaseStream).ToArray();

            if (VerifyPublicKeySignature(keyAlg, payloadData, 0, payloadData.Length -
                signatureData.Length - 4, signature))
            {
                // Raise event to get result of auth attempt.
                var authUserEventArgs = new AuthenticateUserHostBasedEventArgs(userName, clientHostName,
                    clientUserName, keyAlg.ExportPublicKey());

                if (AuthenticateUserHostBased != null) AuthenticateUserHostBased(this, authUserEventArgs);

                // Check result of auth attempt.
                switch (authUserEventArgs.Result)
                {
                    case AuthenticationResult.Success:
                        // Auth has succeeded.
                        AuthenticateUser(serviceName);

                        break;
                    case AuthenticationResult.FurtherAuthRequired:
                        // Auth has succeeded, but further auth is required.
                        SendMsgUserAuthFailure(true);

                        break;
                    case AuthenticationResult.Failure:
                        // Auth has failed.
                        SendMsgUserAuthFailure(false);

                        break;
                }
            }
            else
            {
                // Signature is invalid.
                SendMsgUserAuthFailure(false);
            }
        }

        protected void ProcessMsgUserAuthRequestKeyboardInteractive(string userName, string serviceName,
            SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Read request information.
            string language = msgReader.ReadString();
            string[] subMethods = Encoding.UTF8.GetString(msgReader.ReadByteString()).Split(
                new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            //
        }

        protected void AuthenticateUser(string requestedService)
        {
            // Tell client that auth has succeeded.
            SendMsgUserAuthSuccess();

            // Start requested service.
            _client.StartService(requestedService);

            // Raise event.
            if (UserAuthenticated != null) UserAuthenticated(this, new EventArgs());
        }

        protected bool VerifyPublicKeySignature(PublicKeyAlgorithm alg, byte[] payload, int payloadOffset,
            int payloadCount, byte[] signature)
        {
            using (var hashInputStream = new MemoryStream())
            {
                using (var hashInputWriter = new SshStreamWriter(hashInputStream))
                {
                    // Write input data.
                    hashInputWriter.WriteByteString(_client.SessionId);
                    hashInputWriter.Write(payload, payloadOffset, payloadCount);
                }

                // Verify signature.
                return alg.VerifyData(hashInputStream.ToArray(), signature);
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
                case AuthenticationMethod.KeyboardInteractive:
                    return "keyboard-interactive";
                case AuthenticationMethod.None:
                    return "none";
            }

            return "";
        }
    }

    #region Event Arguments Types
    public class AuthenticateUserNoMethodEventArgs : AuthenticateUserEventArgs
    {
        public AuthenticateUserNoMethodEventArgs(string userName)
            : base(userName)
        {
        }

        public override AuthenticationMethod AuthMethod
        {
            get { return AuthenticationMethod.None; }
        }
    }

    public class AuthenticateUserPublicKeyEventArgs : AuthenticateUserEventArgs
    {
        public AuthenticateUserPublicKeyEventArgs(string userName, SshPublicKey publicKey)
            : base(userName)
        {
            this.PublicKey = publicKey;
            this.Result = AuthenticationResult.Failure;
        }

        public SshPublicKey PublicKey
        {
            get;
            protected set;
        }

        public override AuthenticationMethod AuthMethod
        {
            get { return AuthenticationMethod.PublicKey; }
        }
    }

    public class AuthenticateUserPasswordEventArgs : AuthenticateUserEventArgs
    {
        public AuthenticateUserPasswordEventArgs(string userName, string password)
            : base(userName)
        {
            this.Password = password;
            this.Result = AuthenticationResult.Failure;
        }

        public string Password
        {
            get;
            protected set;
        }

        public override AuthenticationMethod AuthMethod
        {
            get { return AuthenticationMethod.Password; }
        }
    }

    public class AuthenticateUserHostBasedEventArgs : AuthenticateUserEventArgs
    {
        public AuthenticateUserHostBasedEventArgs(string userName, string clientHostName,
            string clientUserName, SshPublicKey publicKey)
            : base(userName)
        {
            this.ClientHostName = clientHostName;
            this.ClientUserName = clientUserName;
            this.PublicKey = publicKey;
        }

        public string ClientHostName
        {
            get;
            protected set;
        }

        public string ClientUserName
        {
            get;
            protected set;
        }

        public SshPublicKey PublicKey
        {
            get;
            protected set;
        }

        public override AuthenticationMethod AuthMethod
        {
            get { return AuthenticationMethod.HostBased; }
        }
    }

    public abstract class AuthenticateUserEventArgs : EventArgs
    {
        public AuthenticateUserEventArgs(string userName)
        {
            this.UserName = userName;
            this.Result = AuthenticationResult.Failure;
        }

        public AuthenticationResult Result
        {
            get;
            set;
        }

        public string UserName
        {
            get;
            protected set;
        }

        public abstract AuthenticationMethod AuthMethod
        {
            get;
        }
    }

    public class ChangePasswordEventArgs : EventArgs
    {
        public ChangePasswordEventArgs(string oldPassword, string newPassword)
        {
            this.OldPassword = oldPassword;
            this.NewPassword = newPassword;
            this.Result = PasswordChangeResult.Failure;
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

        public string ReplyPrompt
        {
            get;
            set;
        }
    }
    #endregion

    public enum AuthenticationResult
    {
        Success,
        FurtherAuthRequired,
        Failure,
        PasswordExpired,
    }

    public enum PasswordChangeResult
    {
        Success,
        FurtherAuthRequired,
        Failure,
        NewPasswordUnacceptable
    }

    public enum AuthenticationMethod
    {
        PublicKey,
        Password,
        HostBased,
        KeyboardInteractive,
        None
    }

    internal enum SshAuthenticationMessage : byte
    {
        Request = 50,
        Failure = 51,
        Success = 52,
        Banner = 53,
        PublicKeyOk = 60,
        PasswordChangeRequired = 60,
        InfoRequest = 61,
        InfoResponse = 62
    }
}
