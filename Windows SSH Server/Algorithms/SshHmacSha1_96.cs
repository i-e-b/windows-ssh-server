using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer.Algorithms
{
    internal class SshHmacSha1_96 : MacAlgorithm
    {
        internal SshHmacSha1_96()
        {
        }

        public override string Name
        {
            get { return "hmac-sha1-96"; }
        }

        public override HMAC CreateAlgorithm()
        {
            var algorithm = new HMACSHA1();

            return algorithm;
        }
    }
}
