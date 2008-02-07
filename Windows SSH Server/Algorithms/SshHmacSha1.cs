using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer.Algorithms
{
    internal class SshHmacSha1 : MacAlgorithm
    {
        internal SshHmacSha1()
            : base()
        {
            _algorithm = new HMACSHA1();
        }

        public override string Name
        {
            get { return "hmac-sha1"; }
        }
    }
}
