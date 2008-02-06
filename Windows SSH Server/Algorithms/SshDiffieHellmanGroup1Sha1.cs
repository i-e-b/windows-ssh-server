using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer.Algorithms
{
    internal class SshDiffieHellmanGroup1Sha1 : KexAlgorithm
    {
        internal SshDiffieHellmanGroup1Sha1()
        {
        }

        public override string Name
        {
            get { return "diffie-hellman-group1-sha1"; }
        }

        public override AsymmetricAlgorithm CreateAlgorithm()
        {
            var algorithm = new DiffieHellmanManaged(1024, 0, DHKeyGeneration.Static);

            return algorithm;
        }
    }
}
