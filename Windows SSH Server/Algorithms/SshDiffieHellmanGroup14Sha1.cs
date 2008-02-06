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
        internal SshDiffieHellmanGroup14Sha1()
        {
        }

        public override string Name
        {
            get { return "diffie-hellman-group14-sha1"; }
        }

        public override AsymmetricAlgorithm CreateAlgorithm()
        {
            var algorithm = new DiffieHellmanManaged(2048, 0, DHKeyGeneration.Static);
            
            return algorithm;
        }
    }
}
