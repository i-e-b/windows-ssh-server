using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer.Algorithms
{
    internal class SshHmacMd5 : MacAlgorithm
    {
        internal SshHmacMd5()
        {
        }

        public override string Name
        {
            get { return "hmac-md5"; }
        }

        public override HMAC CreateAlgorithm()
        {
            var algorithm = new HMACMD5();
            
            return algorithm;
        }
    }
}
