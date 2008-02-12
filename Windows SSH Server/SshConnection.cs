using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    public class SshConnection : SshAuthentication
    {
        private bool _isDisposed = false; // True if object has been disposed.

        public SshConnection(IConnection connection)
            : base(connection)
        {
        }

        public SshConnection(Stream stream)
            : base(stream)
        {
        }

        public SshConnection(IConnection connection, bool addDefaultAlgorithms)
            : base(connection, addDefaultAlgorithms)
        {
        }

        public SshConnection(Stream stream, bool addDefaultAlgorithms)
            : base(stream, addDefaultAlgorithms)
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

        //
    }
}
