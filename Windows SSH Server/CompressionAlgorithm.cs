﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    public abstract class CompressionAlgorithm : IDisposable
    {
        private bool _isDisposed = false; // True if object has been disposed.

        public CompressionAlgorithm()
        {
        }

        ~CompressionAlgorithm()
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

        public abstract byte[] Compress(byte[] input);

        public abstract byte[] Decompress(byte[] input);
    }
}
