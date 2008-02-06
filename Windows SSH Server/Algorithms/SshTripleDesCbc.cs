using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer.Algorithms
{
    internal class SshTripleDesCbc : EncryptionAlgorithm
    {
        internal SshTripleDesCbc()
        {
        }

        public override string Name
        {
            get { return "3des-cbc"; }
        }

        public override SymmetricAlgorithm CreateAlgorithm()
        {
            var algorithm = new TripleDESCryptoServiceProvider();

            algorithm.Mode = CipherMode.CBC;
            
            return algorithm;
        }
    }
}
