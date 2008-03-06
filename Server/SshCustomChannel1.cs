using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SshDotNet;

using ConsoleDotNet;

namespace WindowsSshServer
{
    public class SshCustomChannel1 : SshSessionChannel
    {
        public SshCustomChannel1(ChannelOpenRequestEventArgs requestEventArgs)
            : base(requestEventArgs)
        {
        }

        public SshCustomChannel1(uint senderChannel, uint recipientChannel, uint windowSize,
            uint maxPacketSize)
            : base(senderChannel, recipientChannel, windowSize, maxPacketSize)
        {
        }

        //
    }
}
