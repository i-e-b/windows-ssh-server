using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    public abstract class CompressionAlgorithm
    {
        public abstract string Name
        {
            get;
        }

        public abstract byte[] Compress(byte[] input);

        public abstract byte[] Decompress(byte[] input);
    }
}
