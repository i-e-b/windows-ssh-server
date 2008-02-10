using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    public abstract class MacAlgorithm
    {
        protected HMAC _algorithm;        // Algorithm to use.

        private bool _isDisposed = false; // True if object has been disposed.

        public MacAlgorithm()
        {
        }

        ~MacAlgorithm()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    if (_algorithm != null) _algorithm.Clear();
                }

                // Dispose unmanaged resources.
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract string Name
        {
            get;
        }

        public HMAC Algorithm
        {
            get { return _algorithm; }
        }

        public virtual byte[] ComputeHash(byte[] input)
        {
            return _algorithm.ComputeHash(input);
        }
    }
}
