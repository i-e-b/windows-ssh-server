using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public abstract class SshChannel
    {
        protected bool _eofSent; // True if EOF (end of file) message has been sent.

        protected SshConnectionService _connService; // Connection service.
        //protected uint _senderChannel;             // Channel number for sender.
        //protected uint _recipientChannel;          // Channel number for recipient.

        public SshChannel(ChannelOpenRequestEventArgs requestEventArgs)
            : this(requestEventArgs.ClientChannel, requestEventArgs.ServerChannel,
            requestEventArgs.InitialWindowSize, requestEventArgs.MaxPacketSize)
        {
        }

        public SshChannel(uint clientChannel, uint serverChannel, uint windowSize, uint maxPacketSize)
        {
            _eofSent = false;

            this.ClientChannel = clientChannel;
            this.ServerChannel = serverChannel;
            this.WindowSize = windowSize;
            this.MaxPacketSize = maxPacketSize;
        }

        public uint ClientChannel
        {
            get;
            set;
        }

        public uint ServerChannel
        {
            get;
            set;
        }

        public uint WindowSize
        {
            get;
            protected set;
        }

        public uint MaxPacketSize
        {
            get;
            protected set;
        }

        public SshConnectionService ConnectionService
        {
            get { return _connService; }
            internal set { _connService = value; }
        }

        public void SendEof()
        {
            _connService.SendMsgChannelEof(this);
            _eofSent = true;
        }

        internal void WriteChannelOpenConfirmationData()
        {
        }
    }
}
