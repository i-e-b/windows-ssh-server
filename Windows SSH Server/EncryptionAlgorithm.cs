using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    internal abstract class EncryptionAlgorithm
    {
        protected SymmetricAlgorithm _algorithm; // Algorithm to use.

        internal EncryptionAlgorithm()
        {
        }

        public abstract string Name
        {
            get;
        }

        public SymmetricAlgorithm Algorithm
        {
            get { return _algorithm; }
        }

        public virtual byte[] Encrypt(byte[] input)
        {
            return _algorithm.Encrypt(input);
        }

        public virtual byte[] Decrypt(byte[] input)
        {
            return _algorithm.Decrypt(input);
        }
    }
}
