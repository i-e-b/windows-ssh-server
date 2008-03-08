using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SshDotNet;

using ConsoleDotNet;

namespace WindowsSshServer
{
    public class SshTerminalChannel : SshSessionChannel
    {
        public static string InjectionDllFileName
        {
            get;
            set;
        }

        protected ConsoleHandler _consoleHandler; // Handles Windows console.

        private bool _isDisposed = false;         // True if object has been disposed.

        public SshTerminalChannel(ChannelOpenRequestEventArgs requestEventArgs)
            : base(requestEventArgs)
        {
        }

        public SshTerminalChannel(uint senderChannel, uint recipientChannel, uint windowSize,
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

        public void Initialize()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Create console handler.
            _consoleHandler = new ConsoleHandler();
            _consoleHandler.InjectionDllFileName = SshTerminalChannel.InjectionDllFileName;
            _consoleHandler.Initialize();
        }

        protected override void InternalClose()
        {
            // Dispose console handler.
            if (_consoleHandler != null) _consoleHandler.Dispose();

            base.InternalClose();
        }
    }
}
