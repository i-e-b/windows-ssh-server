using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer.Algorithms
{
    internal class SshDss : PublicKeyAlgorithm
    {
        internal SshDss()
        {
        }

        public override string Name
        {
            get { return "ssh-dss"; }
        }

        public override AsymmetricAlgorithm CreateAlgorithm()
        {
            var algorithm = new DSACryptoServiceProvider();

            return algorithm;
        }
    }
}
