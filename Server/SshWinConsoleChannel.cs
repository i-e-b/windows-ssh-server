using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using SshDotNet;

using ConsoleDotNet;

namespace WindowsSshServer
{
    public class SshWinConsoleChannel : SshSessionChannel
    {
        public static string InjectionDllFileName
        {
            get;
            set;
        }

        protected CHAR_INFO[,] _consoleOldBuffer; // Old buffer of console.

        protected Terminal _terminal;             // Terminal, which translates sent and received data.
        protected ConsoleHandler _consoleHandler; // Handles Windows console.
        //protected Thread _monitorTerminalThread;  // Thread to monitor terminal.

        private bool _isDisposed = false;         // True if object has been disposed.

        public SshWinConsoleChannel(ChannelOpenRequestEventArgs requestEventArgs)
            : base(requestEventArgs)
        {
        }

        public SshWinConsoleChannel(uint senderChannel, uint recipientChannel, uint windowSize,
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
                        //if (_monitorTerminalThread != null) _monitorTerminalThread.Abort();
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

            //// Abort current monitor thread, if one exists.
            //if (_monitorTerminalThread != null) _monitorTerminalThread.Abort();

            //// Create thread to monitor terminal.
            //_monitorTerminalThread = new Thread(new ThreadStart(MonitorTerminal));
            //_monitorTerminalThread.Start();

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
            _consoleHandler.InjectionDllFileName = SshWinConsoleChannel.InjectionDllFileName;

            _consoleHandler.ConsoleWindowResized += new EventHandler<EventArgs>(
                _consoleHandler_ConsoleWindowResized);
            _consoleHandler.ConsoleBufferResized += new EventHandler<EventArgs>(
                _consoleHandler_ConsoleBufferResized);
            _consoleHandler.ConnsoleBufferChanged += new EventHandler<EventArgs>(
                _consoleHandler_ConnsoleBufferChanged);
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

        protected override void OnPseudoTerminalRequested(PseudoTerminalRequestedEventArgs e)
        {
            // Check type of terminal.
            switch (e.TerminalName)
            {
                case "xterm":
                    _terminal = new XtermTerminal();
                    break;
                default:
                    e.Success = false;
                    return;
            }

            e.Success = true;
        }

        protected override void OnPseudoTerminalAllocated(EventArgs e)
        {
            base.OnPseudoTerminalAllocated(e);
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

        protected byte[] ReadNewTerminalData()
        {
            var consoleBuffer = _consoleHandler.GetConsoleBuffer();
            int consoleBufWidth = consoleBuffer.GetLength(1);
            int consoleBufHeight = consoleBuffer.GetLength(0);

            // Create empty old buffer if this is first read from terminal.
            if (_consoleOldBuffer == null)
            {
                _consoleOldBuffer = new CHAR_INFO[consoleBuffer.GetLength(0), consoleBuffer.GetLength(1)];

                for (int y = 0; y < consoleBufHeight; y++)
                    for (int x = 0; x < consoleBufWidth; x++)
                        _consoleOldBuffer[y, x] = new CHAR_INFO() { AsciiChar = ' ', Attributes = 0 };
            }

            try
            {
                using (var outputStream = new MemoryStream())
                {
                    unsafe
                    {
                        var consoleScreenBufInfo = (CONSOLE_SCREEN_BUFFER_INFO*)_consoleHandler.
                            ConsoleScreenInfo.Get();
                        int startLine = consoleScreenBufInfo->srWindow.Top;
                        int endLine = Math.Min(consoleScreenBufInfo->srWindow.Bottom, consoleBufHeight - 1);

                        // Find range of data within console buffer that is new.
                        CHAR_INFO charInfo;
                        CHAR_INFO oldCharInfo;
                        int startX = -1;
                        int startY = -1;
                        int endX = -1;
                        int endY = -1;

                        for (int y = startLine; y <= endLine; y++)
                        {
                            for (int x = 0; x < consoleBufWidth; x++)
                            {
                                charInfo = consoleBuffer[y, x];
                                oldCharInfo = _consoleOldBuffer[y, x];

                                // Check if current buffer differs from old buffer at this position.
                                if (charInfo.AsciiChar != oldCharInfo.AsciiChar)
                                {
                                    if (startX == -1)
                                    {
                                        startX = x;
                                        startY = y;
                                    }

                                    endX = x;
                                    endY = y;
                                }
                            }
                        }

                        if (startX != -1)
                        {
                            // Write new data to output stream.
                            int curX = startX;
                            int curY = startY;

                            while (curY < endY || (curY == endY && curX <= endX))
                            {
                                // Write current ASCII char.
                                outputStream.WriteByte((byte)consoleBuffer[curY, curX].AsciiChar);

                                curX++;
                                if (curX == consoleBufWidth)
                                {
                                    // Write CR LF chars.
                                    outputStream.WriteByte((byte)'\n');
                                    outputStream.WriteByte((byte)'\r');

                                    curX = 0;
                                    curY++;
                                }
                            }
                        }
                    }

                    // Escape new data, and then return it.
                    return _terminal.EscapeData(outputStream.ToArray());
                }
            }
            finally
            {
                // Set old buffer of console.
                _consoleOldBuffer = consoleBuffer;
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

        private void _consoleHandler_ConsoleWindowResized(object sender, EventArgs e)
        {
            //
        }

        private void _consoleHandler_ConsoleBufferResized(object sender, EventArgs e)
        {
            //
        }

        private void _consoleHandler_ConnsoleBufferChanged(object sender, EventArgs e)
        {
            // Send new data on terminal to clinet.
            var newTermData = ReadNewTerminalData();

            SendData(newTermData);
        }
    }
}
