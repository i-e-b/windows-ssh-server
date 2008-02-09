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

        internal KexAlgorithm()
        {
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
