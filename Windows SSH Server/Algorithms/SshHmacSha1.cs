using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer.Algorithms
{
    internal class SshHmacSha1 : MacAlgorithm
    {
        internal SshHmacSha1()
        {
        }

        public override string Name
        {
            get { return "hmac-sha1"; }
        }

        public override HMAC CreateAlgorithm()
        {
            var algorithm = new HMACSHA1();
            
            return algorithm;
        }
    }
}
