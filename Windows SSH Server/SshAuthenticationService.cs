using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text;

namespace WindowsSshServer
{
    public class SshAuthenticationService : SshService
    {
        protected bool _bannerMsgSent;     // True if banner message has already been sent.
        protected int _loginAttempts;      // Number of login attempts made so far.

        protected DateTime _startTime;     // Date/time at which service was started.
        protected Timer _authTimeoutTimer; // Timer to detect when authentication has timed out.

        private bool _isDisposed = false;  // True if object has been disposed.

        public SshAuthenticationService(SshClient client)
            : base(client)
        {
            _loginAttempts = 0;

            // Initialize properties to default values.
            this.Timeout = new TimeSpan(0, 10, 0);
            this.MaximumLoginAttempts = 20;
            this.BannerMessage = null;
            this.BannerMessageLanguage = "";

            // <test>
            this.BannerMessage = System.Windows.Forms.Application.ProductName + " - banner message\r\n" +
                "---------------------------------------\r\n";
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

        internal override bool ProcessMessage(byte[] payload)
        {
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
                        case SshAuthenticationMessage.UserAuthRequest:
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

        internal override void Start()
        {
            // Set time at which service was started.
            _startTime = DateTime.Now;

            // Create timer to detect timeout.
            _authTimeoutTimer = new Timer(new TimerCallback(AuthTimerCallback), null, 0, 1000);
        }

        protected void AuthTimerCallback(object state)
        {
            // Check if authentication has timed out.
            if ((DateTime.Now - _startTime) >= this.Timeout)
            {
                // Dispose timer.
                _authTimeoutTimer.Dispose();
                _authTimeoutTimer = null;

                // Authentication has timed out.
                _client.Disconnect(true);
            }
        }

        protected void SendMsgUserAuthSuccess()
        {
            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshAuthenticationMessage.UserAuthSuccess);
                }

                // Send User Auth Success message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void ProcessMsgUserAuthRequest(SshStreamReader msgReader)
        {
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
                throw new SshDisconnectedException();
            }

            // Check method of authentication.
            switch (methodName)
            {
                case "publickey":
                    ProcessMsgUserAuthRequestPublicKey(msgReader);
                    break;
                case "password":
                    ProcessMsgUserAuthRequestPassword(msgReader);
                    break;
                case "hostbased":
                    ProcessMsgUserAuthRequestHostBased(msgReader);
                    break;
                case "none":
                    ProcessMsgUserAuthRequestNone(msgReader);
                    break;
                default:
                    break;
            }
        }

        protected void ProcessMsgUserAuthRequestPublicKey(SshStreamReader msgReader)
        {
            // Read whether request is actual authentication request.
            bool isAuthRequest = msgReader.ReadBoolean();

            if (isAuthRequest)
            {
                // Request is authentication request.
            }
            else
            {
                // Request is query whether specified method is acceptable.
                string algName = msgReader.ReadString();
                string keyBlob = msgReader.ReadString();
            }
        }

        protected void ProcessMsgUserAuthRequestPassword(SshStreamReader msgReader)
        {
            //
        }

        protected void ProcessMsgUserAuthRequestHostBased(SshStreamReader msgReader)
        {
            //
        }

        protected void ProcessMsgUserAuthRequestNone(SshStreamReader msgReader)
        {
            // Send list of supported authentication methods.
            SendMsgUserAuthFailure(new string[] { "publickey", "password", "hostbased" }, false);
        }

        protected void SendMsgUserAuthFailure(string[] authsMethods, bool partialSuccess)
        {
            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshAuthenticationMessage.UserAuthFailure);

                    // Write authentication information.
                    msgWriter.WriteNameList(authsMethods);
                    msgWriter.Write(partialSuccess);
                }

                // Send User Auth Failure message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgUserAuthBanner(string message, string language)
        {
            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshAuthenticationMessage.UserAuthBanner);

                    // Write banner information.
                    msgWriter.WriteByteString(Encoding.UTF8.GetBytes(message));
                    msgWriter.Write(language);
                }

                // Send User Auth Banner message.
                _client.SendPacket(msgStream.ToArray());
            }
        }
    }

    internal enum SshAuthenticationMessage
    {
        UserAuthRequest = 50,
        UserAuthFailure = 51,
        UserAuthSuccess = 52,
        UserAuthBanner = 53
    }
}
