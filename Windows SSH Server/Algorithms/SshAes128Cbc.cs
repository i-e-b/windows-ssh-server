using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer.Algorithms
{
    internal class SshAes128Cbc : EncryptionAlgorithm
    {
        internal SshAes128Cbc()
        {
        }

        public override string Name
        {
            get { return "aes128-cbc"; }
        }

        public override SymmetricAlgorithm CreateAlgorithm()
        {
            var algorithm = new AesCryptoServiceProvider();

            algorithm.Mode = CipherMode.CBC;
            algorithm.KeySize = 128;
            
            return algorithm;
        }
    }
}
