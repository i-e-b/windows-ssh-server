using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public abstract class SshService : IDisposable
    {
        protected SshClient _client;      // Client for which service is running.

        private bool _isDisposed = false; // True if object has been disposed.

        public SshService(SshClient client)
        {
            _client = client;
        }

        ~SshService()
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
                }

                // Dispose unmanaged resources.
            }

            _isDisposed = true;
        }

        public SshClient Client
        {
            get { return _client; }
        }

        public abstract string Name
        {
            get;
        }

        internal abstract bool ProcessMessage(byte[] payload);

        internal abstract void Start();
    }
}
