using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    public class SshAuthentication : SshTransport
    {
        private bool _isDisposed = false; // True if object has been disposed.

        public SshAuthentication(IConnection connection)
            : base(connection)
        {
        }

        public SshAuthentication(Stream stream)
            : base(stream)
        {
        }

        public SshAuthentication(IConnection connection, bool addDefaultAlgorithms)
            : base(connection, addDefaultAlgorithms)
        {
        }

        public SshAuthentication(Stream stream, bool addDefaultAlgorithms)
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
