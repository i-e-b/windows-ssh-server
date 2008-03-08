using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace ConsoleDotNet
{
    public sealed class ConsoleHandler : IDisposable
    {
        public event EventHandler<EventArgs> ConsoleClose;
        public event EventHandler<EventArgs> ConsoleWindowResize;
        public event EventHandler<EventArgs> ConsoleBufferResize;

        private SharedMemory<ConsoleParams> _consoleParams;
        private SharedMemory<CONSOLE_SCREEN_BUFFER_INFO> _consoleScreenInfo;
        private SharedMemory<CONSOLE_CURSOR_INFO> _consoleCursorInfo;
        private SharedMemory<CHAR_INFO> _consoleBuffer;
        private SharedMemory<ConsoleCopyInfo> _consoleCopyInfo;
        private SharedMemory<UIntPtr> _consolePasteInfo;
        private SharedMemory<MOUSE_EVENT_RECORD> _consoleMouseEvent;
        private SharedMemory<ConsoleSizeInfo> _consoleNewSizeInfo;
        private SharedMemory<SIZE> _consoleNewScrollPos;

        private SafeWaitHandle _procSafeWaitHandle;
        private Thread _monitorThread;

        private IntPtr _hProcess;
        private IntPtr _hKernel32;
        private IntPtr _procBaseAddress;
        private PROCESS_INFORMATION _procInfo;
        private Process _process;
        private int _threadId;
        private IntPtr _hModule;

        private bool _isDisposed = false; // True if object has been disposed.

        public ConsoleHandler()
        {
            // Create objects for shared memory.
            _consoleParams = new SharedMemory<ConsoleParams>();
            _consoleScreenInfo = new SharedMemory<CONSOLE_SCREEN_BUFFER_INFO>();
            _consoleCursorInfo = new SharedMemory<CONSOLE_CURSOR_INFO>();
            _consoleBuffer = new SharedMemory<CHAR_INFO>();
            _consoleCopyInfo = new SharedMemory<ConsoleCopyInfo>();
            _consolePasteInfo = new SharedMemory<UIntPtr>();
            _consoleMouseEvent = new SharedMemory<MOUSE_EVENT_RECORD>();
            _consoleNewSizeInfo = new SharedMemory<ConsoleSizeInfo>();
            _consoleNewScrollPos = new SharedMemory<SIZE>();
        }

        ~ConsoleHandler()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                }

                // Dispose unmanaged resources.

                // Dispose wait handle.
                if (_procSafeWaitHandle != null) _procSafeWaitHandle.Dispose();

                // Abort monitor thread.
                if (_monitorThread != null) _monitorThread.Abort();

                // Close console window.
                unsafe
                {
                    try
                    {
                        ConsoleParams* consoleParams = (ConsoleParams*)_consoleParams.Get();

                        if (consoleParams->ConsoleWindowHandle != IntPtr.Zero)
                            WinApi.SendMessage(consoleParams->ConsoleWindowHandle, WinApi.WM_CLOSE,
                                IntPtr.Zero, IntPtr.Zero);
                    }
                    catch (AccessViolationException)
                    {
                    }
                }

                // Dispose shared memory objects.
                if (_consoleParams != null) _consoleParams.Dispose();
                if (_consoleScreenInfo != null) _consoleScreenInfo.Dispose();
                if (_consoleCursorInfo != null) _consoleCursorInfo.Dispose();
                if (_consoleBuffer != null) _consoleBuffer.Dispose();
                if (_consoleCopyInfo != null) _consoleCopyInfo.Dispose();
                if (_consolePasteInfo != null) _consolePasteInfo.Dispose();
                if (_consoleMouseEvent != null) _consoleMouseEvent.Dispose();
                if (_consoleNewSizeInfo != null) _consoleNewSizeInfo.Dispose();
                if (_consoleNewScrollPos != null) _consoleNewScrollPos.Dispose();

                //// Kill console process.
                //if (_process != null)
                //{
                //    _process.Kill();
                //    _process.Dispose();
                //}
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public SharedMemory<ConsoleParams> ConsoleParameters
        {
            get { return _consoleParams; }
        }

        public SharedMemory<CONSOLE_SCREEN_BUFFER_INFO> ConsoleScreenInfo
        {
            get { return _consoleScreenInfo; }
        }

        public SharedMemory<CONSOLE_CURSOR_INFO> ConsoleCursorInfo
        {
            get { return _consoleCursorInfo; }
        }

        public SharedMemory<CHAR_INFO> ConsoleBuffer
        {
            get { return _consoleBuffer; }
        }

        public SharedMemory<ConsoleCopyInfo> ConsoleCopyInfo
        {
            get { return _consoleCopyInfo; }
        }

        public SharedMemory<UIntPtr> ConsolePasteInfo
        {
            get { return _consolePasteInfo; }
        }

        public SharedMemory<MOUSE_EVENT_RECORD> ConsoleMouseEvent
        {
            get { return _consoleMouseEvent; }
        }

        public SharedMemory<ConsoleSizeInfo> ConsoleNewSizeInfo
        {
            get { return _consoleNewSizeInfo; }
        }

        public SharedMemory<SIZE> ConsoleNewScrollPos
        {
            get { return _consoleNewScrollPos; }
        }

        public IntPtr ProcessHandle
        {
            get { return _hProcess; }
            set { _hProcess = value; }
        }

        public Process Process
        {
            get { return _process; }
        }

        public int ThreadId
        {
            get { return _threadId; }
            set { _threadId = value; }
        }

        public string InjectionDllFileName
        {
            get;
            set;
        }

        public void Initialize()
        {
            // Start new console process.
            StartProcess();

            // Inject DLL into console process.
            InjectDll(this.InjectionDllFileName);

            // Resume main thread of console process.
            WinApi.ResumeThread(_procInfo.hThread);
            WinApi.CloseHandle(_procInfo.hThread);

            // Wait for DLL to set console handle.
            int waitRet = WinApi.WaitForSingleObject(_consoleParams.RequestEvent.SafeWaitHandle
                .DangerousGetHandle(), 1000);
            if (waitRet == WinApi.WAIT_FAILED) throw new Win32Exception();
            if (waitRet == WinApi.WAIT_TIMEOUT) throw new TimeoutException();

            // Create wait handle for console process.
            _procSafeWaitHandle = new SafeWaitHandle(_process.Handle, false);

            // Set language of console window.
            unsafe
            {
                ConsoleParams* consoleParams = (ConsoleParams*)_consoleParams.Get();

                if (!WinApi.PostMessage(consoleParams->ConsoleWindowHandle, WinApi.WM_INPUTLANGCHANGEREQUEST,
                    IntPtr.Zero, new IntPtr(CultureInfo.CurrentCulture.KeyboardLayoutId)))
                    throw new Win32Exception();
            }

            // Start thread to monitor console.
            _monitorThread = new Thread(new ThreadStart(MonitorThread));
            _monitorThread.Start();

            // Resume thread to monitor hook.
            unsafe
            {
                ConsoleParams* consoleParams = (ConsoleParams*)_consoleParams.Get();
                IntPtr hHookThread = WinApi.OpenThread(ThreadAccess.ALL_ACCESS, false,
                    consoleParams->HookThreadId);

                if (WinApi.ResumeThread(hHookThread) == -1) throw new Win32Exception();
                WinApi.CloseHandle(hHookThread);
            }
        }

        public ConsoleParams GetConsoleParameters()
        {
            unsafe
            {
                return *((ConsoleParams*)_consoleParams.Get());
            }
        }

        public CONSOLE_SCREEN_BUFFER_INFO GetConsoleScreenInfo()
        {
            unsafe
            {
                return *((CONSOLE_SCREEN_BUFFER_INFO*)_consoleScreenInfo.Get());
            }
        }

        public CONSOLE_CURSOR_INFO GetConsoleCursorInfo()
        {
            unsafe
            {
                return *((CONSOLE_CURSOR_INFO*)_consoleCursorInfo.Get());
            }
        }

        public CHAR_INFO[,] GetConsoleBuffer()
        {
            unsafe
            {
                CONSOLE_SCREEN_BUFFER_INFO* consoleScreenInfo = (CONSOLE_SCREEN_BUFFER_INFO*)
                    _consoleScreenInfo.Get();
                CHAR_INFO[,] buffer = new CHAR_INFO[consoleScreenInfo->dwSize.X,
                    consoleScreenInfo->dwSize.Y];
                
                fixed (CHAR_INFO* dst = buffer)
                {
                    WinApi.CopyMemory(new IntPtr(dst), new IntPtr(_consoleBuffer.Get()), buffer.Length
                        * Marshal.SizeOf(typeof(CHAR_INFO)));
                }

                return buffer;
            }
        }

        public ConsoleCopyInfo GetConsoleCopyInfo()
        {
            unsafe
            {
                return *((ConsoleCopyInfo*)_consoleCopyInfo.Get());
            }
        }

        public UIntPtr GetConsolePasteInfo()
        {
            unsafe
            {
                return *((UIntPtr*)_consolePasteInfo.Get());
            }
        }

        public MOUSE_EVENT_RECORD GetConsoleMouseEvent()
        {
            unsafe
            {
                return *((MOUSE_EVENT_RECORD*)_consoleMouseEvent.Get());
            }
        }

        private void MonitorThread()
        {
            ProcessWaitHandle procWaitHandle = null;
            WaitHandle[] waitHandles;

            try
            {
                unsafe
                {
                    // Get pointers to shared memory objects.
                    ConsoleParams* consoleParams = (ConsoleParams*)_consoleParams.Get();
                    CONSOLE_SCREEN_BUFFER_INFO* consoleScreenInfo = (CONSOLE_SCREEN_BUFFER_INFO*)
                        _consoleScreenInfo.Get();

                    // Keep waiting for new events until process has exitted or thread is aborted.
                    procWaitHandle = new ProcessWaitHandle(_procSafeWaitHandle);
                    waitHandles = new WaitHandle[] { procWaitHandle, _consoleBuffer.RequestEvent };

                    while (WaitHandle.WaitAny(waitHandles) > 0)
                    {
                        // Get current window and buffer size.
                        int columns = consoleScreenInfo->srWindow.Right
                            - consoleScreenInfo->srWindow.Left + 1;
                        int rows = consoleScreenInfo->srWindow.Bottom
                            - consoleScreenInfo->srWindow.Top + 1;
                        int bufferColumns = consoleScreenInfo->dwSize.X;
                        int bufferRows = consoleScreenInfo->dwSize.Y;

                        // Check if window size has changed.
                        if (consoleParams->Columns != columns || consoleParams->Rows != rows)
                        {
                            consoleParams->Columns = columns;
                            consoleParams->Rows = rows;

                            Console.WriteLine("New window size: {0}x{1}", consoleParams->Columns,
                                consoleParams->Rows);
                        }

                        // Check if buffer size has changed.
                        if ((consoleParams->BufferColumns != 0
                            && consoleParams->BufferColumns != bufferColumns) ||
                            (consoleParams->BufferRows != 0 && consoleParams->BufferRows != bufferRows))
                        {
                            consoleParams->BufferColumns = bufferColumns;
                            consoleParams->BufferRows = bufferRows;

                            Console.WriteLine("New buffer size: {0}x{1}", consoleParams->BufferColumns,
                                consoleParams->BufferRows);
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
            }
            finally
            {
                if (procWaitHandle != null) procWaitHandle.Close();

                // Raise event: console has closed.
                if (ConsoleClose != null) ConsoleClose(this, new EventArgs());
            }
        }

        private void StartProcess()
        {
            bool retValue;

            // Create startup info for new console process.
            STARTUPINFO startupInfo = new STARTUPINFO();

            startupInfo.cb = Marshal.SizeOf(startupInfo);
            startupInfo.dwFlags = StartFlags.STARTF_USESHOWWINDOW;
            startupInfo.wShowWindow = ShowWindow.Show;
            startupInfo.lpTitle = "Console";

            SECURITY_ATTRIBUTES procAttrs = new SECURITY_ATTRIBUTES();
            SECURITY_ATTRIBUTES threadAttrs = new SECURITY_ATTRIBUTES();

            procAttrs.nLength = Marshal.SizeOf(procAttrs);
            threadAttrs.nLength = Marshal.SizeOf(threadAttrs);

            retValue = WinApi.CreateProcess(null, "powershell", ref procAttrs, ref threadAttrs, false,
                CreationFlags.CREATE_NEW_CONSOLE | CreationFlags.CREATE_SUSPENDED, IntPtr.Zero, null,
                ref startupInfo, out _procInfo);
            if (!retValue) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Unable to create new console process.");

            // Start new console process.
            _process = Process.GetProcessById(_procInfo.dwProcessId);

            // Create objects in shared memory.
            CreateSharedObjects(_procInfo.dwProcessId);

            // Write startup parameters to shared memory.
            unsafe
            {
                ConsoleParams* consoleParams = (ConsoleParams*)_consoleParams.Get();

                consoleParams->ConsoleMainThreadId = _procInfo.dwThreadId;
                consoleParams->ParentProcessId = Process.GetCurrentProcess().Id;
                consoleParams->NotificationTimeout = 10;
                consoleParams->RefreshInterval = 100;
                consoleParams->Rows = 25;
                consoleParams->Columns = 80;
                consoleParams->BufferRows = 200;
                consoleParams->BufferColumns = 80;
            }
        }

        private void InjectDll(string dllFileName)
        {
            bool retValue;

            // Get handle to target process.
            _hProcess = WinApi.OpenProcess(ProcessAccessFlags.All, false, _procInfo.dwProcessId);
            if (_hProcess == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),
                string.Format("Cannot open process with ID {0}.", _procInfo.dwProcessId));

            // Get address of LoadLibrary function in kernel32.
            _hKernel32 = WinApi.GetModuleHandle("kernel32");
            if (_hKernel32 == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Cannot get handle to kernel32 module.");

            // LoadLibraryA (ascii)/LoadLibraryW (unicode)
            IntPtr addrLoadLibrary = WinApi.GetProcAddress(_hKernel32, "LoadLibraryA");
            if (addrLoadLibrary == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Cannot find LoadLibrary function in kernel32 module.");

            // Write file name of DLL into process memory.
            _procBaseAddress = WinApi.VirtualAllocEx(_hProcess, IntPtr.Zero, dllFileName.Length,
                WinApi.MEM_COMMIT, WinApi.PAGE_READWRITE);
            if (_procBaseAddress == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Error allocating virtual memory in target process.");

            int bytesWritten;
            retValue = WinApi.WriteProcessMemory(_hProcess, _procBaseAddress,
                Encoding.ASCII.GetBytes(dllFileName), dllFileName.Length, out bytesWritten);
            if (!retValue) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Error writing DLL file name in target process memory.");

            // Create remote thread to load library into target process.
            IntPtr hThread = WinApi.CreateRemoteThread(_hProcess, IntPtr.Zero, 0, addrLoadLibrary,
                _procBaseAddress, 0, out _threadId);
            if (hThread == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Error creating remote thread in target process.");

            // Wait for thread to finish.
            int waitRet = WinApi.WaitForSingleObject(hThread, 10000);
            if (waitRet == WinApi.WAIT_FAILED) throw new Win32Exception();
            if (waitRet == WinApi.WAIT_TIMEOUT) throw new TimeoutException();

            // Get handle to loaded module from exit code.
            // Note: exit code will be zero unless DLL has already terminated.
            int exitCode;

            WinApi.GetExitCodeThread(hThread, out exitCode);
            _hModule = (IntPtr)exitCode;

            // Clean up.
            WinApi.VirtualFreeEx(_hProcess, _procBaseAddress, dllFileName.Length, WinApi.MEM_RELEASE);
            WinApi.CloseHandle(hThread);
        }

        private void EjectDll()
        {
            // Get address of FreeLibrary function in kernel32.
            IntPtr addrFreeLibrary = WinApi.GetProcAddress(_hKernel32, "FreeLibrary");
            if (addrFreeLibrary == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Cannot find FreeLibrary function in kernel32 module.");

            // Create remote thread to free library from target process.
            int threadId;
            IntPtr hThread = WinApi.CreateRemoteThread(_hProcess, IntPtr.Zero, 0, addrFreeLibrary,
                _hModule, 0, out threadId);
            if (hThread == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Error creating remote thread in target process.");

            // Wait for thread to finish.
            int waitRet = WinApi.WaitForSingleObject(hThread, 10000);
            if (waitRet == WinApi.WAIT_FAILED) throw new Win32Exception();
            if (waitRet == WinApi.WAIT_TIMEOUT) throw new TimeoutException();

            // Clean up.
            WinApi.CloseHandle(hThread);
        }

        private void CreateSharedObjects(int consoleProcessId)
        {
            // Create objects in shared memory.
            _consoleParams.Create(string.Format("Console2_params_{0}", consoleProcessId), 1,
                SyncObjectTypes.SyncObjRequest);
            _consoleScreenInfo.Create(string.Format("Console2_consoleInfo_{0}", consoleProcessId), 1,
                SyncObjectTypes.SyncObjRequest);
            _consoleCursorInfo.Create(string.Format("Console2_cursorInfo_{0}", consoleProcessId), 1,
                SyncObjectTypes.SyncObjRequest);
            _consoleBuffer.Create(string.Format("Console2_consoleBuffer_{0}", consoleProcessId), 200 * 200,
                SyncObjectTypes.SyncObjRequest);
            _consoleCopyInfo.Create(string.Format("Console2_consoleCopyInfo_{0}", consoleProcessId), 1,
                SyncObjectTypes.SyncObjBoth);
            _consolePasteInfo.Create(string.Format("Console2_consolePasteInfo_{0}", consoleProcessId), 1,
                SyncObjectTypes.SyncObjBoth);
            _consoleMouseEvent.Create(string.Format("Console2_formatMouseEvent_{0}", consoleProcessId), 1,
                SyncObjectTypes.SyncObjBoth);
            _consoleNewSizeInfo.Create(string.Format("Console2_newConsoleSize_{0}", consoleProcessId), 1,
                SyncObjectTypes.SyncObjRequest);
            _consoleNewScrollPos.Create(string.Format("Console2_newScrollPos_{0}", consoleProcessId), 1,
                SyncObjectTypes.SyncObjRequest);

            // Initialize console buffer.
            unsafe
            {
                CHAR_INFO charInfo;

                charInfo.Attributes = 0;
                charInfo.UnicodeChar = ' ';
                for (int i = 0; i < 200 * 200; ++i)
                    WinApi.CopyMemory(new IntPtr(_consoleBuffer.Get(i)), new IntPtr(&charInfo),
                        sizeof(CHAR_INFO));
            }
        }
    }
}
