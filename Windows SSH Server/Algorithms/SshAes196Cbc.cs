using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer.Algorithms
{
    internal class SshAes196Cbc : EncryptionAlgorithm
    {
        internal SshAes196Cbc()
        {
        }

        public override string Name
        {
            get { return "aes196-cbc"; }
        }

        public override SymmetricAlgorithm CreateAlgorithm()
        {
            var algorithm = new AesCryptoServiceProvider();

            algorithm.Mode = CipherMode.CBC;
            algorithm.KeySize = 196;
            
            return algorithm;
        }
    }
}
