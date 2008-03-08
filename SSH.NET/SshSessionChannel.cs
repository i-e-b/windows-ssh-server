using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public class SshSessionChannel : SshChannel
    {
        protected string _termEnvVar;            // TERM environment variable.
        protected List<TerminalMode> _termModes; // List of active terminal modes.

        public SshSessionChannel(ChannelOpenRequestEventArgs requestEventArgs)
            : base(requestEventArgs)
        {
        }

        public SshSessionChannel(uint senderChannel, uint recipientChannel, uint windowSize,
            uint maxPacketSize)
            : base(senderChannel, recipientChannel, windowSize, maxPacketSize)
        {
        }

        protected internal override void ProcessRequest(string requestType, bool wantReply, 
            SshStreamReader msgReader)
        {
            switch (requestType)
            {
                case "pty-req":
                    // Read information for pseudo-terminal request.
                    var termEnvVar = msgReader.ReadString();
                    var termCharsWidth = msgReader.ReadUInt32();
                    var termCharsHeight = msgReader.ReadUInt32();
                    var termPixelsWidth = msgReader.ReadUInt32();
                    var termPixelsHeight = msgReader.ReadUInt32();
                    var termModes = msgReader.ReadString();

                    //

                    _termEnvVar = termEnvVar;

                    if (wantReply) _connService.SendMsgChannelSuccess(this);

                    break;
                case "shell":
                    if (wantReply) _connService.SendMsgChannelSuccess(this);

                    break;
                default:
                    base.ProcessRequest(requestType, wantReply, msgReader);
                    break;
            }
        }
    }
}
