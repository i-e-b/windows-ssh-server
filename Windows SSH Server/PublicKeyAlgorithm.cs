using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    internal abstract class PublicKeyAlgorithm
    {
        public abstract string Name
        {
            get;
        }

        public abstract byte[] SignHash(byte[] hashData);
    }
}
