using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public abstract class SshChannel
    {
        //protected uint _senderChannel;    // Channel number for sender.
        //protected uint _recipientChannel; // Channel number for recipient.

        public SshChannel(uint senderChannel, uint recipientChannel, uint windowSize, uint maxPacketSize)
        {
            this.SenderChannel = senderChannel;
            this.RecipientChannel = recipientChannel;
            this.WindowSize = windowSize;
            this.MaxPacketSize = maxPacketSize;
        }

        public uint SenderChannel
        {
            get;
            set;
        }

        public uint RecipientChannel
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

        //
    }
}
