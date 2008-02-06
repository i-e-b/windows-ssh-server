using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer.Algorithms
{
    internal class SshRsa : PublicKeyAlgorithm
    {
        internal SshRsa()
        {
        }

        public override string Name
        {
            get { return "ssh-rsa"; }
        }

        public override AsymmetricAlgorithm CreateAlgorithm()
        {
            var algorithm = new RSACryptoServiceProvider();

            return algorithm;
        }
    }
}
