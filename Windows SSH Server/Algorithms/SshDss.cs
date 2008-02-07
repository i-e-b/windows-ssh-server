using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer.Algorithms
{
    internal class SshDss : PublicKeyAlgorithm
    {
        protected DSACryptoServiceProvider _algorithm; // Algorithm to use.

        internal SshDss()
            : base()
        {
            _algorithm = new DSACryptoServiceProvider();
        }

        public override string Name
        {
            get { return "ssh-dss"; }
        }

        public override byte[] SignHash(byte[] hashData)
        {
            throw new NotImplementedException();
        }
    }
}
