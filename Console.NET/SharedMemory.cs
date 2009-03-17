using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace ConsoleDotNet
{
    public class SharedMemory<T> : IDisposable where T : new()
    {
        private string _name;
        private int _size;
        private IntPtr _hSharedMemory;
        private IntPtr _pSharedMemory;
        private Mutex _sharedMutex;
        private EventWaitHandle _sharedReqEvent;
        private EventWaitHandle _sharedRespEvent;
        
        private bool _isDisposed = false; // True if object has been disposed.

        public SharedMemory(string name, int size, SyncObjectTypes syncObjects, bool create)
        {
            if (create)
                Create(name, size, syncObjects);
            else
                Open(name, syncObjects);
        }

        public SharedMemory()
        {
        }

        ~SharedMemory()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                }

                // Dispose unmanaged resources.
                if (_hSharedMemory != IntPtr.Zero) WinApi.CloseHandle(_hSharedMemory);
                if (_pSharedMemory != IntPtr.Zero) WinApi.UnmapViewOfFile(_pSharedMemory);

                if (_sharedMutex != null) _sharedMutex.Close();
                if (_sharedReqEvent != null) _sharedReqEvent.Close();
                if (_sharedRespEvent != null) _sharedRespEvent.Close();
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int Size
        {
            get { return _size; }
        }

        public EventWaitHandle RequestEvent
        {
            get
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                return _sharedReqEvent;
            }
        }

        public EventWaitHandle ResponseEvent
        {
            get
            {
                if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

                return _sharedRespEvent;
            }
        }

        public bool IsDisposed
        {
            get { return _isDisposed; }
        }

        public unsafe void* Get(int index)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            return (void*)(_pSharedMemory.ToInt32() + index * Marshal.SizeOf(typeof(T)));
        }

        public unsafe void* Get()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            return (void*)_pSharedMemory;
        }

        public unsafe void Set(void* data)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            void** pMemory = (void**)_pSharedMemory;
            *pMemory = data;
        }

        public void Lock()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            if (_sharedMutex == null) return;
            _sharedMutex.WaitOne();
        }

        public void Release()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            if (_sharedMutex == null) return;
            _sharedMutex.ReleaseMutex();
        }

        public void Create(string name, int size, SyncObjectTypes syncObjects)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            _name = name;
            _size = size;

            // Create file mapping.
            _hSharedMemory = WinApi.CreateFileMapping((IntPtr)WinApi.INVALID_HANDLE_VALUE, IntPtr.Zero,
                PageProtection.ReadWrite, 0, _size * Marshal.SizeOf(typeof(T)), name);
            if (_hSharedMemory == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Error creating file mapping.");

            // Get base address of file mapping.
            _pSharedMemory = WinApi.MapViewOfFile(_hSharedMemory, SectionAccessFlags.SECTION_ALL_ACCESS,
                0, 0, IntPtr.Zero);
            if (_pSharedMemory == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Error getting bsae address of file mapping.");

            // Zero file-mapped memory.
            WinApi.ZeroMemory(_pSharedMemory, _size * Marshal.SizeOf(typeof(T)));

            // Create synchronisation objects.
            if (syncObjects != SyncObjectTypes.SyncObjNone) CreateSyncObjects(syncObjects, name);
        }

        public void Open(string name, SyncObjectTypes syncObjects)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            _name = name;

            // Open file mapping.
            _hSharedMemory = WinApi.OpenFileMapping(SectionAccessFlags.SECTION_ALL_ACCESS, false, _name);
            if (_hSharedMemory == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Error opening file mapping.");

            // Get base address of file mapping.
            _pSharedMemory = WinApi.MapViewOfFile(_hSharedMemory, SectionAccessFlags.SECTION_ALL_ACCESS,
                0, 0, IntPtr.Zero);
            if (_pSharedMemory == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Error getting bsae address of file mapping.");

            // Create synchronisation objects.
            if (syncObjects != SyncObjectTypes.SyncObjNone) CreateSyncObjects(syncObjects, name);

        }

        private void CreateSyncObjects(SyncObjectTypes syncObjects, string name)
        {
            // Create synchronisation objects.
            if (syncObjects >= SyncObjectTypes.SyncObjRequest)
            {
                _sharedMutex = new Mutex(false, name + "_mutex");
                _sharedReqEvent = new EventWaitHandle(false, EventResetMode.AutoReset,
                    name + "_req_event");
            }

            if (syncObjects >= SyncObjectTypes.SyncObjBoth)
            {
                _sharedRespEvent = new EventWaitHandle(false, EventResetMode.AutoReset,
                    name + "_resp_event");
            }
        }
    }

    public class SharedMemoryLock<T> : IDisposable where T : new()
    {
        protected SharedMemory<T> _sharedMemory;

        public SharedMemoryLock(SharedMemory<T> sharedMemory)
        {
            // Get lock on memory.
            _sharedMemory.Lock();
        }

        public void Dispose()
        {
            // Release lock on memory.
            _sharedMemory.Release();
        }
    }

    public enum SyncObjectTypes
    {
        SyncObjNone = 0,
        SyncObjRequest = 1,
        SyncObjBoth = 2
    }
}
