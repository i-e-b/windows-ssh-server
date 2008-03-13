using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public class SshSessionChannel : SshChannel
    {
        public event EventHandler<EventArgs> PseudoTerminalAllocated;

        protected string _termEnvVar;                  // TERM environment variable.
        protected uint _termCharsWidth;                // Width of terminal, in chars.
        protected uint _termCharsHeight;               // Height of terminal, in chars.
        protected uint _termPixelsWidth;               // Width of terminal, in pixels.
        protected uint _termPixelsHeight;              // Height of terminal, in pixels.
        protected Dictionary<string, string> _envVars; // List of environment variables to pass to shell.
        protected List<TerminalMode> _termModes;       // List of active terminal modes.

        private bool _isDisposed = false;              // True if object has been disposed.

        public SshSessionChannel(ChannelOpenRequestEventArgs requestEventArgs)
            : base(requestEventArgs)
        {
        }

        public SshSessionChannel(uint senderChannel, uint recipientChannel, uint windowSize,
            uint maxPacketSize)
            : base(senderChannel, recipientChannel, windowSize, maxPacketSize)
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

        public IEnumerable<KeyValuePair<string, string>> EnvironmentVars
        {
            get { return _envVars.AsEnumerable(); }
        }

        public ReadOnlyCollection<TerminalMode> TerminalModes
        {
            get { return _termModes.AsReadOnly(); }
        }

        protected internal override void ProcessRequest(string requestType, bool wantReply,
            SshStreamReader msgReader)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            switch (requestType)
            {
                case "pty-req":
                    // Read information about pseudo-terminal.
                    _termEnvVar = msgReader.ReadString();
                    _termCharsWidth = msgReader.ReadUInt32();
                    _termCharsHeight = msgReader.ReadUInt32();
                    _termPixelsWidth = msgReader.ReadUInt32();
                    _termPixelsHeight = msgReader.ReadUInt32();

                    // Get list of terminal modes.
                    ReadTerminalModes(msgReader.ReadByteString());

                    // Add TERM to list of environment variables.
                    _envVars.Add("TERM", _termEnvVar);

                    // Raise event.
                    OnPseudoTerminalAllocated(new EventArgs());

                    if (wantReply) _connService.SendMsgChannelSuccess(this);

                    return;
                case "env":
                    // Read name and value of environment variable.
                    var varName = msgReader.ReadString();
                    var varValue = msgReader.ReadString();

                    // Add variable to list.
                    _envVars.Add(varName, varValue);

                    if (wantReply) _connService.SendMsgChannelSuccess(this);

                    return;
                case "shell":
                    // Start default shell.
                    StartShell();

                    if (wantReply) _connService.SendMsgChannelSuccess(this);

                    return;
                case "exec":
                    // not implemented

                    break;
                default:
                    base.ProcessRequest(requestType, wantReply, msgReader);
                    return;
            }

            // Request has failed.
            if (wantReply) _connService.SendMsgChannelFailure(this);
        }

        protected virtual void StartShell()
        {
            // empty
        }

        protected internal override void Open(SshConnectionService connService)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            _envVars = new Dictionary<string, string>();
            _termModes = new List<TerminalMode>();

            base.Open(connService);
        }

        protected virtual void OnPseudoTerminalAllocated(EventArgs e)
        {
            if (PseudoTerminalAllocated != null) PseudoTerminalAllocated(this, e);
        }

        protected void ReadTerminalModes(byte[] encodedModes)
        {
            TerminalModeOpCode opCode;

            // Clear current list of terminal modes.
            _termModes.Clear();

            // Read modes from encoded byte stream.
            using (var streamReader = new SshStreamReader(new MemoryStream(encodedModes)))
            {
                while (true)
                {
                    opCode = (TerminalModeOpCode)streamReader.ReadByte();

                    if (opCode == TerminalModeOpCode.TtyOpEnd) break;
                    if ((byte)opCode >= 1 && (byte)opCode <= 160)
                    {
                        // Add mode to list.
                        _termModes.Add(new TerminalMode(opCode, streamReader.ReadUInt32()));
                    }
                    else
                    {
                        // Undefined op code.
                        break;
                    }
                }
            }
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
