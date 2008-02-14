using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public class SshConnectionService : SshService
    {
        private bool _isDisposed = false; // True if object has been disposed.

        public SshConnectionService(SshClient client)
            : base(client)
        {
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

        public override string Name
        {
            get { return "ssh-connection"; }
        }

        internal override bool ProcessMessage(byte[] payload)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            //throw new NotImplementedException();
            return false;
        }

        internal override void Start()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            //throw new NotImplementedException();
        }

        protected void SendMsgGlobalRequest(string requestName, bool wantReply, byte[] data)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshConnectionMessage.GlobalRequest);

                    // Write request information.
                    msgWriter.Write(requestName);
                    msgWriter.Write(wantReply);

                    if (data != null) msgWriter.Write(data);
                }

                // Send Global Request message.
                _client.SendPacket(msgStream.ToArray());
            }
        }
    }

    internal enum SshConnectionMessage : byte
    {
        GlobalRequest = 80,
        RequestSuccess = 81,
        RequestFailure = 82,
        ChannelOpen = 90,
        ChannelOpenConfirmation = 91,
        ChannelOpenFailure = 92,
        ChannelWindowAdjust = 93,
        ChannelData = 94,
        ChannelExtendedData = 95,
        ChannelEof = 96,
        ChannelClose = 97,
        ChannelRequest= 98,
        ChannelSuccess = 99,
        ChannelFailure = 100,
    }
}
