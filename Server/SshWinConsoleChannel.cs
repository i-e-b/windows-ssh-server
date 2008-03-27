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

        //protected CHAR_INFO[,] _consoleOldBuffer;       // Old buffer of all data of console.
        protected CHAR_INFO[,] _consoleOldScreenBuffer; // Old screen buffer of console.
        protected SMALL_RECT _consoleOldWindow;         // Old window bounds of console.
        protected COORD _consoleOldCursorPos;           // Old position of cursor in console.
        protected int _consoleOldStartIndex;            // Start index of old new data in console.
        protected int _consoleOldEndIndex;              // End index of old new data in console.

        protected Terminal _terminal;                   // Terminal, which translates sent and received data.
        protected ConsoleHandler _consoleHandler;       // Handles Windows console.
        //protected Thread _monitorTerminalThread;        // Thread to monitor terminal.

        private bool _isDisposed = false;               // True if object has been disposed.

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

        public int BitMode
        {
            get;
            set;
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
            // Set bit mode.
            if (_termModes.Exists(mode => mode.OpCode == TerminalModeOpCode.CS7))
                _terminal.BitMode = TerminalBitMode.Mode7Bit;
            else if (_termModes.Exists(mode => mode.OpCode == TerminalModeOpCode.CS8))
                _terminal.BitMode = TerminalBitMode.Mode8Bit;

            //// Create buffer for console data.
            //unsafe
            //{
            //    var screenBufInfo = (CONSOLE_SCREEN_BUFFER_INFO*)_consoleHandler.ConsoleScreenInfo.Get();

            //    _consoleOldBuffer = new CHAR_INFO[screenBufInfo->dwSize.Y, screenBufInfo->dwSize.X];
            //}

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
            unsafe
            {
                var screenBuffer = _consoleHandler.GetConsoleBuffer();
                var screenBufWidth = screenBuffer.GetLength(1);
                var screenBufHeight = screenBuffer.GetLength(0);
                var screenBufInfo = (CONSOLE_SCREEN_BUFFER_INFO*)_consoleHandler.ConsoleScreenInfo.Get();
                var window = screenBufInfo->srWindow;
                var cursorPos = screenBufInfo->dwCursorPosition;

                if (window.Left < _consoleOldWindow.Left || window.Top < _consoleOldWindow.Top) return null;

                cursorPos.X -= (ushort)window.Left;
                cursorPos.Y -= (ushort)window.Top;

                int startIndex = -1;
                int endIndex = -1;

                // Create empty old buffer if this is first read from terminal.
                if (_consoleOldScreenBuffer == null)
                {
                    _consoleOldScreenBuffer = new CHAR_INFO[screenBuffer.GetLength(0), screenBuffer.GetLength(1)];

                    for (int y = 0; y < screenBufHeight; y++)
                        for (int x = 0; x < screenBufWidth; x++)
                            _consoleOldScreenBuffer[y, x] = new CHAR_INFO() { AsciiChar = ' ', Attributes = 0 };
                }

                try
                {
                    // Create stream to which to output new terminal data.
                    using (var outputStream = new MemoryStream())
                    {
                        // Scan console buffer to find range of chars that are new/changed.
                        CHAR_INFO charInfo;
                        CHAR_INFO oldCharInfo;
                        int startX = -1, startY = -1;
                        int endX = 0; int endY = 0;
                        int x, y = 0;

                        for (int oldY = window.Top - _consoleOldWindow.Top; oldY < screenBufHeight; oldY++)
                        {
                            x = 0;

                            for (int oldX = window.Left - _consoleOldWindow.Left; oldX < screenBufWidth;
                                oldX++)
                            {
                                charInfo = screenBuffer[y, x];
                                oldCharInfo = _consoleOldScreenBuffer[oldY, oldX];

                                // Check if current buffer differs from old buffer at this position.
                                if (charInfo.AsciiChar != oldCharInfo.AsciiChar)
                                {
                                    if (startX == -1)
                                    {
                                        startX = x;
                                        startY = y;
                                    }

                                    if (charInfo.AsciiChar != ' ')
                                    {
                                        endX = x;
                                        endY = y;
                                    }
                                }

                                x++;
                            }

                            y++;
                        }

                        // Check if new data was found.
                        if (startX != -1 || !window.Equals(_consoleOldWindow) ||
                            !cursorPos.Equals(_consoleOldCursorPos))
                        {
                            int cursorIndex = cursorPos.Y * screenBufWidth + cursorPos.X;

                            if (startX == -1)
                            {
                                startX = _consoleOldCursorPos.X;
                                startY = _consoleOldCursorPos.Y;
                                startIndex = _consoleOldCursorPos.Y * screenBufWidth + _consoleOldCursorPos.X;
                                endIndex = -1;
                            }
                            else
                            {
                                startIndex = startY * screenBufWidth + startX;
                                endIndex = endY * screenBufWidth + endX;
                            }

                            startIndex = Math.Min(startIndex, _consoleOldEndIndex + 1);

                            // If cursor is beyond end of range, adjust end index.
                            if (cursorIndex - 1 > endIndex) endIndex = cursorIndex - 1;

                            // Write required number of DEL chars to output stream.
                            int numBackspaces = Math.Max(_consoleOldEndIndex - startIndex + 1, 0);

                            for (int i = 0; i < numBackspaces; i++)
                                outputStream.WriteByte(127); // DEL

                            //// Erase all chars to right of beginning of new data.
                            //if (numBackspaces > 0)
                            //{
                            //    outputStream.WriteByte(27);
                            //    outputStream.WriteByte((byte)'[');
                            //    outputStream.WriteByte((byte)(numBackspaces).ToString()[0]);
                            //    outputStream.WriteByte((byte)'X');
                            //}

                            // Write new data to output stream.
                            int curX = startX;
                            int curY = startY;
                            int newCharsCount = endIndex - startIndex + 1;
                            int charIndex = 0;

                            while (charIndex < newCharsCount)
                            {
                                // Write current ASCII char.
                                outputStream.WriteByte((byte)screenBuffer[curY, curX].AsciiChar);
                                charIndex++;

                                curX++;
                                if (curX == screenBufWidth)
                                {
                                    curX = 0;
                                    curY++;

                                    // Write CR LF chars.
                                    outputStream.WriteByte((byte)'\r');
                                    outputStream.WriteByte((byte)'\n');
                                }
                            }
                        }

                        // Escape new data, and then return it.
                        return _terminal.EscapeData(outputStream.ToArray());
                    }
                }
                finally
                {
                    // Set old console info.
                    _consoleOldScreenBuffer = screenBuffer;
                    _consoleOldWindow = window;
                    _consoleOldCursorPos = cursorPos;
                    _consoleOldStartIndex = startIndex;
                    _consoleOldEndIndex = endIndex;
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

            if (newTermData != null) SendData(newTermData);
        }
    }
}
