using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer.Algorithms
{
    internal class SshRsa : PublicKeyAlgorithm
    {
        protected RSACryptoServiceProvider _algorithm; // Algorithm to use.

        internal SshRsa()
            : base()
        {
            _algorithm = new RSACryptoServiceProvider();
        }

        public override string Name
        {
            get { return "ssh-rsa"; }
        }

        public override byte[] SignHash(byte[] hashData)
        {
            throw new NotImplementedException();
        }
    }
}
