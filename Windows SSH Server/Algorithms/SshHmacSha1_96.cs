using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer.Algorithms
{
    internal class SshHmacSha1_96 : SshHmacSha1
    {
        internal SshHmacSha1_96()
            : base()
        {
        }

        public override string Name
        {
            get { return "hmac-sha1-96"; }
        }

        public override byte[] ComputeHash(byte[] input)
        {
            var hash = base.ComputeHash(input);
            Array.Resize(ref hash, 12); // 12 bytes = 96 bits

            return hash;
        }
    }
}
