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
        protected int _loginAttempts;      // Number of login attempts made so far.

        protected DateTime _startTime;     // Date/time at which service was started.
        protected Timer _authTimeoutTimer; // Timer to detect when authentication has timed out.

        public SshAuthenticationService(SshClient client)
            : base(client)
        {
            _loginAttempts = 0;

            // Initialize properties to default values.
            this.Timeout = new TimeSpan(0, 10, 0);
            this.MaximumLoginAttempts = 20;
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

        public override string Name
        {
            get { return "ssh-userauth"; }
        }

        internal override bool ProcessMessage(byte[] payload)
        {
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
                // Authentication has timed out.
                _client.Disconnect(true);
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

            //
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
