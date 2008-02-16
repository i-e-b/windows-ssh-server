using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public class SshSessionChannel : SshChannel
    {
        public SshSessionChannel(uint senderChannel, uint recipientChannel, uint windowSize,
            uint maxPacketSize)
            : base(senderChannel, recipientChannel, windowSize, maxPacketSize)
        {
            //
        }

        //
    }
}
