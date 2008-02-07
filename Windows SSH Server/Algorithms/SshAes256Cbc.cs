using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer.Algorithms
{
    internal class SshAes256Cbc : EncryptionAlgorithm
    {
        internal SshAes256Cbc()
            : base()
        {
            _algorithm = new AesCryptoServiceProvider();
            _algorithm.Mode = CipherMode.CBC;
            _algorithm.KeySize = 256;
        }

        public override string Name
        {
            get { return "aes256-cbc"; }
        }
    }
}
