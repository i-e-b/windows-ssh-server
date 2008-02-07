using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer.Algorithms
{
    internal class SshZlibCompression : CompressionAlgorithm
    {
        internal SshZlibCompression()
            : base()
        {
        }

        public override string Name
        {
            get { return "zlib"; }
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
