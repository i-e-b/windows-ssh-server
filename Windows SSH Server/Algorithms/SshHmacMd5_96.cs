using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer.Algorithms
{
    internal class SshHmacMd5_96 : MacAlgorithm
    {
        internal SshHmacMd5_96()
        {
        }

        public override string Name
        {
            get { return "hmac-md5-96"; }
        }

        public override HMAC CreateAlgorithm()
        {
            var algorithm = new HMACMD5();
            
            return algorithm;
        }
    }
}
