using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public abstract class SshChannel : IDisposable
    {
        public event EventHandler<EventArgs> Opened;
        public event EventHandler<EventArgs> EofSent;
        public event EventHandler<EventArgs> EofReceived;
        public event EventHandler<EventArgs> Closed;
        public event EventHandler<EventArgs> DataReceived;

        protected bool _eofSent;                     // True if EOF (end of file) message has been sent.
        protected bool _eofReceived;                 // True if EOF (end of file) message has been received.

        protected SshConnectionService _connService; // Connection service.
        
        private bool _isDisposed = false;            // True if object has been disposed.

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

        ~SshChannel()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    InternalClose();
                }

                // Dispose unmanaged resources.
            }

            _isDisposed = true;
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
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            _connService.SendMsgChannelEof(this);
            _eofSent = true;

            // Raise event.
            OnEofSent(new EventArgs());
        }

        public void Close()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            InternalClose();   
        }

        protected internal virtual void Open(SshConnectionService connService)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            _connService = connService;

            // Raise event.
            OnOpened(new EventArgs());
        }

        protected virtual void InternalClose()
        {
            // Send close message if client is connected.
            if (_connService.Client.IsConnected) _connService.SendMsgChannelClose(this);

            // Raise event.
            OnClosed(new EventArgs());
        }

        protected internal virtual void ProcessEof()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            _eofReceived = true;

            // Raise event.
            OnEofReceived(new EventArgs());
        }

        protected internal virtual void ProcessRequest(string requestType, bool wantReply, SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

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

        protected internal virtual void ProcessWindowAdjust(uint bytesToRead)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            //
        }

        protected internal virtual void ProcessData(byte[] data)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Raise event.
            OnDataReceived(new EventArgs());
        }

        protected internal virtual void ProcessExtendedData(SshExtendedDataType dataType, byte[] data)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Raise event.
            OnDataReceived(new EventArgs());
        }

        protected internal virtual void ProcessSignal(string signalName)
        {
            // empty
        }

        protected internal virtual void WriteChannelOpenConfirmationData()
        {
            // empty
        }

        protected virtual void OnOpened(EventArgs e)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            if (Opened != null) Opened(this, e);
        }

        protected virtual void OnEofSent(EventArgs e)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            if (EofSent != null) EofSent(this, e);
        }

        protected virtual void OnEofReceived(EventArgs e)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            if (EofReceived != null) EofReceived(this, e);
        }
        
        protected virtual void OnClosed(EventArgs e)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            if (Closed != null) Closed(this, e);
        }

        protected virtual void OnDataReceived(EventArgs e)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            if (DataReceived != null) DataReceived(this, e);
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
