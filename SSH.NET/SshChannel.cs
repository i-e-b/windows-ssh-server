using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public abstract class SshChannel
    {
        public event EventHandler<EventArgs> Opened;
        public event EventHandler<EventArgs> EofSent;
        public event EventHandler<EventArgs> EofReceived;
        public event EventHandler<EventArgs> Closed;

        protected List<TerminalMode> _termModes;     // List of active terminal modes.

        protected bool _eofSent;                     // True if EOF (end of file) message has been sent.
        protected bool _eofReceived;                 // True if EOF (end of file) message has been received.

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

        public bool IsEofSent
        {
            get { return _eofSent; }
        }

        public bool IsEofReceived
        {
            get { return _eofReceived; }
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
        }

        public void SendEof()
        {
            _connService.SendMsgChannelEof(this);
            _eofSent = true;

            // Raise event.
            if (EofSent != null) EofSent(this, new EventArgs());
        }

        public virtual void Close()
        {
            _connService.SendMsgChannelClose(this);

            // Raise event.
            if (Closed != null) Closed(this, new EventArgs());
        }

        internal virtual void Open(SshConnectionService connService)
        {
            _connService = connService;

            // Raise event.
            if (Opened != null) Opened(this, new EventArgs());
        }

        internal virtual void ProcessEof()
        {
            _eofReceived = true;

            // Raise event.
            if (EofReceived != null) EofReceived(this, new EventArgs());
        }

        internal virtual void ProcessRequest(string requestType, bool wantReply, SshStreamReader msgReader)
        {
            switch (requestType)
            {
                case "signal":
                    // Process signal.
                    var signalName = msgReader.ReadString();

                    ProcessSignal(signalName);

                    break;
                default:
                    // Unrecognised request type.
                    _connService.SendMsgChannelFailure(this);
                    break;
            }
        }

        internal virtual void ProcessSignal(string signalName)
        {
            // empty
        }

        internal virtual void WriteChannelOpenConfirmationData()
        {
            // empty
        }
    }

    public struct TerminalMode
    {
        public TerminalModeOpCode OpCode;
        public uint Argument;

        public TerminalMode(TerminalModeOpCode opCode, uint argument)
        {
            this.OpCode = opCode;
            this.Argument = argument;
        }
    }

    public enum TerminalModeOpCode : byte
    {
        TtyOpEnd = 0,

        VIntr = 1,
        VQuit,
        VErase,
        VKill,
        VEof,
        VEol,
        VEol2,
        VStart,
        VStop,
        VSusp,
        VDSusp,
        VReprint,
        VWErase,
        VLNext,
        VFlush,
        VSwitch,
        VStatus,
        VDiscard,

        IgnPar = 30,
        ParMrk,
        InPCk,
        IStrip,
        INLCR,
        IgnCR,
        ICRNL,
        IUCLC,
        IXOn,
        IXAny,
        IXOff,
        IMaxBel,

        ISig = 50,
        ICanon,
        XCase,
        Echo,
        EchoE,
        EchoK,
        EchoNL,
        NoFlsh,
        TOStop,
        IExten,
        EchoCtl,
        EchoKE,
        PendIn,

        OPost = 70,
        OLCUC,
        ONLCR,
        OCRNL,
        ONLRet,

        CS7 = 90,
        CS8,
        ParEnb,
        ParOdd,

        TtyOpISpeed = 128,
        TtyOpOSpeed
    }
}
