using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer.Algorithms
{
    internal class SshTripleDesCbc : EncryptionAlgorithm
    {
        internal SshTripleDesCbc()
            : base()
        {
            _algorithm = new TripleDESCryptoServiceProvider();
            _algorithm.Mode = CipherMode.CBC;
        }

        public override string Name
        {
            get { return "3des-cbc"; }
        }
    }
}
