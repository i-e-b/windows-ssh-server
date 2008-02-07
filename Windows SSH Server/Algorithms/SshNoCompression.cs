using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer.Algorithms
{
    internal class SshNoCompression : CompressionAlgorithm
    {
        internal SshNoCompression()
            : base()
        {
        }

        public override string Name
        {
            get { return "none"; }
        }

        public override byte[] Compress(byte[] input)
        {
            return input;
        }

        public override byte[] Decompress(byte[] input)
        {
            return input;
        }
    }
}
