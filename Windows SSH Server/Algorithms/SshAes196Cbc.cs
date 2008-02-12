using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer.Algorithms
{
    public class SshAes196Cbc : EncryptionAlgorithm
    {
        public SshAes196Cbc()
            : base()
        {
            _algorithm = new AesCryptoServiceProvider();
            _algorithm.Mode = CipherMode.CBC;
            _algorithm.KeySize = 192;
        }

        public override string Name
        {
            get { return "aes196-cbc"; }
        }

        public override object Clone()
        {
            return new SshAes196Cbc();
        }
    }
}
