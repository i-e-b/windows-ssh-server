using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer.Algorithms
{
    internal class SshDiffieHellmanGroup14Sha1 : KexAlgorithm
    {
        protected DiffieHellman _algorithm; // Algorithm to use.

        internal SshDiffieHellmanGroup14Sha1()
            : base()
        {
            _algorithm = new DiffieHellmanManaged(2048, 0, DHKeyGeneration.Static);
        }

        public override string Name
        {
            get { return "diffie-hellman-group14-sha1"; }
        }

        public override byte[] CreateKeyExchange()
        {
            return _algorithm.CreateKeyExchange();
        }

        public override byte[] DecryptKeyExchange(byte[] exchangeData)
        {
            return _algorithm.DecryptKeyExchange(exchangeData);
        }
    }
}
