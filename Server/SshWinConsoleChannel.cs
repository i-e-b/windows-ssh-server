using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ConsoleDotNet;
using SshDotNet;

namespace WindowsSshServer
{
    public class SshWinConsoleChannel : SshSessionChannel
    {
        public static string InjectionDllFileName
        {
            get;
            set;
        }

        //protected SMALL_RECT _consoleOldWindow;  // Old window bounds of console.
        //protected COORD _consoleOldCursorPos;    // Old position of cursor in console.
        protected int _consoleOldBufferSize;       // Old size of new data in screen buffer.
        protected int _consoleOldBufferStartIndex; // Old start index of new data in screen buffer.
        protected int _consoleOldBufferEndIndex;   // Old end index of new data in screen buffer.
        protected int _consoleOldCursorIndex;      // Old index of cursor in console.

        //protected string _terminalTitle;         // Title of terminal instance.
        protected Terminal _terminal;              // Terminal, which translates sent and received data.
        protected ConsoleHandler _consoleHandler;  // Handles Windows console.

        private bool _isDisposed = false;          // True if object has been disposed.

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

        public string TerminalTitle
        {
            get { return (_consoleHandler == null) ? null : _consoleHandler.ConsoleTitle; }
        }

        public bool TerminalVisible
        {
            get
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                if (_consoleHandler == null) return false;
                return _consoleHandler.ConsoleVisible;
            }
            set
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                if (_consoleHandler == null) return;
                var oldValue = _consoleHandler.ConsoleVisible;
                _consoleHandler.ConsoleVisible = value;

                if (value != oldValue)
                    OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("TerminalVisible"));
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

            // Initialize console (start process and configure handler).
            _consoleHandler.EnvironmentVars = _envVars;
            _consoleHandler.ConsoleInitialWindowWidth = (int)_termCharsWidth;
            _consoleHandler.ConsoleInitialWindowHeight = (int)_termCharsHeight;
            _consoleHandler.ConsoleInitialBufferWidth = _consoleHandler.ConsoleInitialWindowWidth;
            _consoleHandler.ConsoleInitialBufferHeight = 0x1000;

            _consoleHandler.Initialize();

            _consoleOldBufferSize = 0;
            _consoleOldBufferStartIndex = 0;
            _consoleOldBufferEndIndex = 0;
            _consoleOldCursorIndex = 0;

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

            // Create console handler.
            _consoleHandler = new ConsoleHandler("powershell");
            _consoleHandler.ConsoleTitle = string.Format("{0} - channel {1}",
                connService.Client.Connection.ToString(), this.ServerChannel);
            _consoleHandler.InjectionDllFileName = SshWinConsoleChannel.InjectionDllFileName;

            _consoleHandler.ConsoleWindowResized += new EventHandler<EventArgs>(
                _consoleHandler_ConsoleWindowResized);
            _consoleHandler.ConsoleBufferResized += new EventHandler<EventArgs>(
                _consoleHandler_ConsoleBufferResized);
            _consoleHandler.ConsoleNewData += new EventHandler<EventArgs>(_consoleHandler_ConsoleNewData);
            _consoleHandler.ConsoleBufferChanged += new EventHandler<EventArgs>(
                _consoleHandler_ConnsoleBufferChanged);
            _consoleHandler.ConsoleCursorPositionChanged += new EventHandler<EventArgs>(
                _consoleHandler_ConsoleCursorPositionChanged);

            base.Open(connService);
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
                var bufferInfo = (ConsoleBufferInfo*)_consoleHandler.ConsoleBufferInfo.Get();
                var bufferEndIndex = bufferInfo->BufferStartIndex + bufferInfo->BufferSize;
                var buffer = _consoleHandler.GetConsoleBuffer();
                var screenInfo = (CONSOLE_SCREEN_BUFFER_INFO*)_consoleHandler.ConsoleScreenInfo.Get();
                var window = screenInfo->srWindow;
                var cursorPos = screenInfo->dwCursorPosition;
                var cursorIndex = cursorPos.Y * screenInfo->dwSize.X + cursorPos.X;

                try
                {
                    // Create stream to which to output new terminal data.
                    using (var outputStream = new MemoryStream())
                    {
                        if (bufferInfo->NewDataFound)
                        {
                            System.Diagnostics.Trace.WriteLine(string.Format(
                                "buffer start index: {0}, size: {1}",
                                bufferInfo->BufferStartIndex, bufferInfo->BufferSize));

                            // Write required number of backspaces/spaces to output stream.
                            int numSpaces = bufferInfo->BufferStartIndex - _consoleOldCursorIndex;

                            for (int i = 0; i > numSpaces; i--)
                            {
                                outputStream.WriteByte(0x08);
                                outputStream.WriteByte(0x1b); // ESC
                                outputStream.WriteByte(0x9b); // CSI
                                outputStream.WriteByte((byte)'K');
                            }

                            for (int i = 0; i < numSpaces; i++)
                                outputStream.WriteByte(0x20); // Space char

                            // Write all new data to output stream.
                            var charBuffer = (from c in buffer select (byte)c.UnicodeChar).ToArray();

                            foreach (var c in buffer)
                            {
                                outputStream.WriteByte((byte)c.UnicodeChar);
                            }

                            _consoleOldCursorIndex = bufferEndIndex;
                        }

                        if (bufferInfo->CursorPositionChanged)
                        {
                            // Write control seq to move cursor forward/backward.
                            int cursorOffset = cursorIndex - _consoleOldCursorIndex;

                            if (cursorOffset != 0)
                            {
                                outputStream.WriteByte(0x1b); // ESC
                                outputStream.WriteByte(0x9b); // CSI
                                outputStream.WriteDigits(Math.Abs(cursorOffset));
                                if (cursorOffset > 0) outputStream.WriteByte((byte)'C');
                                if (cursorOffset < 0) outputStream.WriteByte((byte)'D');
                            }
                        }

                        // Escape new data, and then return it.
                        return _terminal.EscapeData(outputStream.ToArray());
                    }
                }
                finally
                {
                    //// Store old console info.
                    //_consoleOldWindow = window;
                    //_consoleOldCursorPos = cursorPos;
                    _consoleOldBufferSize = bufferInfo->BufferSize;
                    _consoleOldBufferStartIndex = bufferInfo->BufferStartIndex;
                    _consoleOldBufferEndIndex = bufferEndIndex;
                    _consoleOldCursorIndex = cursorIndex;

                    // Set response event.
                    _consoleHandler.ConsoleBufferInfo.ResponseEvent.Set();
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

        public override string ToString()
        {
            return this.TerminalTitle;
        }

        private void _consoleHandler_ConsoleWindowResized(object sender, EventArgs e)
        {
            // TO DO
        }

        private void _consoleHandler_ConsoleBufferResized(object sender, EventArgs e)
        {
            // TO DO
        }

        private void _consoleHandler_ConsoleNewData(object sender, EventArgs e)
        {
            // Read new data from terminal buffer.            
            var newTermData = ReadNewTerminalData();

            if (!_connService.Client.IsConnected) return;

            // Send new data of terminal to client.
            if (newTermData != null) SendData(newTermData);
        }

        private void _consoleHandler_ConnsoleBufferChanged(object sender, EventArgs e)
        {
        }

        private void _consoleHandler_ConsoleCursorPositionChanged(object sender, EventArgs e)
        {
        }
    }
}
