using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    public abstract class KexAlgorithm
    {
        protected HashAlgorithm _hashAlgorithm; // Algorithm to use for hashing.

        private bool _isDisposed = false;       // True if object has been disposed.

        public KexAlgorithm()
        {
        }

        ~KexAlgorithm()
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
                    if (_hashAlgorithm != null) _hashAlgorithm.Clear();
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

        public abstract AsymmetricAlgorithm ExchangeAlgorithm
        {
            get;
        }

        public HashAlgorithm HashAlgorithm
        {
            get { return _hashAlgorithm; }
        }

        public abstract byte[] CreateKeyExchange();

        public abstract byte[] DecryptKeyExchange(byte[] exchangeData);

        public byte[] ComputeHash(byte[] input)
        {
            return _hashAlgorithm.ComputeHash(input);
        }
    }
}
