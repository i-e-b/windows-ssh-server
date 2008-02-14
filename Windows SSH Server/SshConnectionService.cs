using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    public class SshConnectionService : SshService
    {
        private bool _isDisposed = false; // True if object has been disposed.

        public SshConnectionService(SshClient client) : base(client)
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

        public override string Name
        {
            get { return "ssh-connection"; }
        }

        internal override bool ProcessMessage(byte[] payload)
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            throw new NotImplementedException();
        }

        internal override void Start()
        {
            if (_isDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            throw new NotImplementedException();
        }
    }
}
