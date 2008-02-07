using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer.Algorithms
{
    internal class SshHmacMd5 : MacAlgorithm
    {
        internal SshHmacMd5()
            : base()
        {
            _algorithm = new HMACMD5();
        }

        public override string Name
        {
            get { return "hmac-md5"; }
        }
    }
}
