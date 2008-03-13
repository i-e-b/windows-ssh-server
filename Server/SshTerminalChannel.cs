using System;
using System.Collections.Generic;
using System.IO;
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

        protected Terminal _terminal;             // Terminal, which translates sent and received data.
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

        public bool TerminalVisible
        {
            get
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                return _consoleHandler.ConsoleVisible;
            }
            set
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                _consoleHandler.ConsoleVisible = value;
            }
        }

        protected override void StartShell()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            _consoleHandler.EnvironmentVars = _envVars;
            _consoleHandler.ConsoleInitialWindowWidth = (int)_termCharsWidth;
            _consoleHandler.ConsoleInitialWindowHeight = (int)_termCharsHeight;
            _consoleHandler.ConsoleInitialBufferWidth = _consoleHandler.ConsoleInitialWindowWidth;
            _consoleHandler.ConsoleInitialBufferHeight = 0x1000;

            // Initialize console (start process and configure handler).
            _consoleHandler.Initialize();

            base.StartShell();
        }

        protected override void Open(SshConnectionService connService)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            base.Open(connService);

            // Create console handler.
            _consoleHandler = new ConsoleHandler("powershell");
            _consoleHandler.ConsoleTitle = string.Format("{0} - channel {1}",
                _connService.Client.Connection.ToString(), this.ServerChannel);
            _consoleHandler.InjectionDllFileName = SshTerminalChannel.InjectionDllFileName;
        }

        protected override void InternalClose()
        {
            // Dispose console handler.
            if (_consoleHandler != null) _consoleHandler.Dispose();

            base.InternalClose();
        }

        protected override void ProcessData(byte[] data)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            /*
             * Note: Need to send WM_KEYDOWN messages instead (in order to send special chars like arrows)?
             * Check code in Console2 project.
            */
            // Write unescaped data to console.
            WriteTerminalData(_terminal.UnescapeData(data));

            if (!_isDisposed) base.ProcessData(data);
        }

        protected override void ProcessExtendedData(SshExtendedDataType dataType, byte[] data)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Write unescaped data to console.
            WriteTerminalData(_terminal.UnescapeData(data));

            if (!_isDisposed) base.ProcessExtendedData(dataType, data);
        }

        protected void WriteTerminalData(byte[] data)
        {
            // Paste received data to console.
            var pasteInfo = _consoleHandler.ConsolePasteInfo;

            unsafe
            {
                try
                {
                    pasteInfo.Lock();

                    // Allocate memory for pasting data.
                    IntPtr pRemoteMemory = WinApi.VirtualAllocEx(_consoleHandler.ProcessHandle, IntPtr.Zero,
                        data.Length, WinApi.MEM_COMMIT, WinApi.PAGE_READWRITE);

                    if (pRemoteMemory == IntPtr.Zero) return;

                    int numBytesWritten;

                    if (!WinApi.WriteProcessMemory(_consoleHandler.ProcessHandle, pRemoteMemory, data,
                        data.Length, out numBytesWritten))
                    {
                        // Free allocated memory.
                        WinApi.VirtualFreeEx(_consoleHandler.ProcessHandle, pRemoteMemory, 0,
                            WinApi.MEM_RELEASE);
                        return;
                    }

                    // Set address of data to paste.
                    pasteInfo.Set((void*)pRemoteMemory);
                }
                finally
                {
                    if (!pasteInfo.IsDisposed) pasteInfo.Release();
                }

                // Signal request and wait for response.
                pasteInfo.RequestEvent.Set();
                pasteInfo.ResponseEvent.WaitOne();
            }
        }

        protected byte[] ReadNewTerminalData()
        {
            //

            return null;
        }

        protected override void OnPseudoTerminalAllocated(EventArgs e)
        {
            // Check type of terminal.
            switch (_termEnvVar)
            {
                case "xterm":
                    _terminal = new XtermTerminal();
                    break;
            }

            base.OnPseudoTerminalAllocated(e);
        }
    }
}
