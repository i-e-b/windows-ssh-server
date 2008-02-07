using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    internal abstract class KexAlgorithm
    {
        internal KexAlgorithm()
        {
        }

        public abstract string Name
        {
            get;
        }

        public abstract byte[] CreateKeyExchange();

        public abstract byte[] DecryptKeyExchange(byte[] exchangeData);
    }
}
