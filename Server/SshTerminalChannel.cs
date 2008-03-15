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

            // Write unescaped data to console.
            TerminalSendKeys(_terminal.UnescapeData(data));

            if (!_isDisposed) base.ProcessData(data);
        }

        protected override void ProcessExtendedData(SshExtendedDataType dataType, byte[] data)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            // Write unescaped data to console.
            TerminalSendKeys(_terminal.UnescapeData(data));

            if (!_isDisposed) base.ProcessExtendedData(dataType, data);
        }

        protected void TerminalPasteData(byte[] data)
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

        protected void TerminalSendKeys(KeyData[] keys)
        {
            unsafe
            {
                var consoleParams = (ConsoleParams*)_consoleHandler.ConsoleParameters.Get();

                foreach (var key in keys)
                {
                    // Get virtual-key code for key.
                    IntPtr vKey = new IntPtr(key.IsVirtualKey ? key.Value : WinApi.VkKeyScan(
                        (char)key.Value));

                    // Send Key Down and then Key Up messages to console window.
                    WinApi.PostMessage(consoleParams->ConsoleWindowHandle, WinApi.WM_KEYDOWN, vKey,
                        CreateWmKeyDownLParam(1, 0, false, false, 0));
                    WinApi.PostMessage(consoleParams->ConsoleWindowHandle, WinApi.WM_KEYUP, vKey,
                        CreateWmKeyDownLParam(1, 0, false, true, 1));
                }
            }
        }

        protected IntPtr CreateWmKeyDownLParam(int repeatCount, byte scanCode, bool extendedKey,
            bool keyWasDown, int transitionState)
        {
            return new IntPtr(
                repeatCount                   // 0-15  repeat count
                | (scanCode << 16)            // 16-23 scan code
                | (extendedKey ? 1 : 0 << 24) // 24    extended key
                | 0                           // 25-28 reserved
                | 0                           // 29    context code
                | (keyWasDown ? 1 : 0 << 30)  // 30    previous key state
                | (transitionState << 31)     // 31    transition state
                );
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
