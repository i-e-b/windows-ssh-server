using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public class SshConnectionService : SshService
    {
        public event EventHandler<EventArgs> ChannelOpened;
        public event EventHandler<EventArgs> ChannelClosed;

        protected List<SshChannel> _channels; // List of all open channels.

        private bool _isDisposed = false;     // True if object has been disposed.

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

            // Create memory stream from payload data.
            using (var msgStream = new MemoryStream(payload))
            {
                var msgReader = new SshStreamReader(msgStream);

                // Check message ID.
                SshConnectionMessage messageId = (SshConnectionMessage)msgReader.ReadByte();

                switch (messageId)
                {
                    // Global request messages
                    case SshConnectionMessage.GlobalRequest:
                        ProcessMsgGlobalRequest(msgReader);
                        break;
                    case SshConnectionMessage.RequestSuccess:
                        ProcessMsgRequestSuccess(msgReader);
                        break;
                    case SshConnectionMessage.RequestFailure:
                        ProcessMsgRequestFailure(msgReader);
                        break;
                    // Channel messages
                    case SshConnectionMessage.ChannelOpen:
                        ProcessMsgChannelOpen(msgReader);
                        break;
                    case SshConnectionMessage.ChannelOpenConfirmation:
                        ProcessMsgChannelOpenConfirmation(msgReader);
                        break;
                    case SshConnectionMessage.ChannelOpenFailure:
                        ProcessMsgChannelOpenFailure(msgReader);
                        break;
                    case SshConnectionMessage.ChannelEof:
                        ProcessMsgChannelEof(msgReader);
                        break;
                    case SshConnectionMessage.ChannelClose:
                        ProcessMsgChannelClose(msgReader);
                        break;
                    // Unrecognised message
                    default:
                        return false;
                }
            }

            // Message was recognised.
            return true;
        }

        internal override void Start()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            base.Start();
        }

        internal override void Stop()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            base.Stop();
        }

        protected void SendMsgGlobalRequest(string requestName, bool wantReply, byte[] data)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                var msgWriter = new SshStreamWriter(msgStream);

                // Write message ID.
                msgWriter.Write((byte)SshConnectionMessage.GlobalRequest);

                // Write request information.
                msgWriter.Write(requestName);
                msgWriter.Write(wantReply);

                if (data != null) msgWriter.Write(data);

                // Send Global Request message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgChannelOpenConfirmation(SshChannel channel)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                var msgWriter = new SshStreamWriter(msgStream);

                // Write message ID.
                msgWriter.Write((byte)SshConnectionMessage.ChannelOpenConfirmation);

                //

                // Send Channel Open Confirmation message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgChannelOpenFailure(uint recipientChannel, SshChannelOpenFailureReason reason, 
            string description, string language)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                var msgWriter = new SshStreamWriter(msgStream);

                // Write message ID.
                msgWriter.Write((byte)SshConnectionMessage.ChannelOpenFailure);

                // Write information.
                msgWriter.Write(recipientChannel);
                msgWriter.Write((uint)reason);
                msgWriter.WriteByteString(Encoding.UTF8.GetBytes(description));
                msgWriter.Write(language);

                // Send Channel Open Failure message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgChannelEof(SshChannel channel)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                var msgWriter = new SshStreamWriter(msgStream);

                // Write message ID.
                msgWriter.Write((byte)SshConnectionMessage.ChannelEof);

                // Write channel number.
                msgWriter.Write(channel.RecipientChannel);

                // Send Channel EOF message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgChannelClose(SshChannel channel)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                var msgWriter = new SshStreamWriter(msgStream);

                // Write message ID.
                msgWriter.Write((byte)SshConnectionMessage.ChannelClose);

                // Write channel number.
                msgWriter.Write(channel.RecipientChannel);

                // Send Channel Close message.
                _client.SendPacket(msgStream.ToArray());
            }
        }

        protected void ProcessMsgGlobalRequest(SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Read request information.
            string requestName = msgReader.ReadString();
            bool wantReply = msgReader.ReadBoolean();

            //
        }

        protected void ProcessMsgRequestSuccess(SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            //
        }

        protected void ProcessMsgRequestFailure(SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            //
        }

        protected void ProcessMsgChannelOpen(SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Read channel information.
            string channelType = msgReader.ReadString();
            uint senderChannel = msgReader.ReadUInt32();
            uint initialWindowSize = msgReader.ReadUInt32();
            uint maxPacketSize = msgReader.ReadUInt32();

            // Check channel type.
            switch (channelType)
            {
                case "session":
                    //
                    break;
                default:
                    SendMsgChannelOpenFailure(senderChannel, SshChannelOpenFailureReason.UnknownChannelType,
                        string.Format("Channel type {0} is unknown.", channelType), "");
                    return;
            }

            // Raise event.
            if (ChannelOpened != null) ChannelOpened(this, new EventArgs());
        }

        protected void ProcessMsgChannelOpenConfirmation(SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            //
        }

        protected void ProcessMsgChannelOpenFailure(SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            //
        }

        protected void ProcessMsgChannelEof(SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            //
        }

        protected void ProcessMsgChannelClose(SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            //

            // Raise event.
            if (ChannelClosed != null) ChannelClosed(this, new EventArgs());
        }
    }

    public enum SshChannelOpenFailureReason : uint
    {
        AdministrativelyProhibited = 1,
        ConnectFailed = 2,
        UnknownChannelType = 3,
        ResourceShortage = 4
    }

    internal enum SshExtendedDataType : uint
    {
        StdErr = 1
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
        ChannelRequest = 98,
        ChannelSuccess = 99,
        ChannelFailure = 100,
    }
}
