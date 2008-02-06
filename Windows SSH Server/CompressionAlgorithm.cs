using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using WindowsSshServer.Algorithms;

namespace WindowsSshServer
{
    internal abstract class CompressionAlgorithm
    {
        static CompressionAlgorithm()
        {
            CompressionAlgorithm.AllAlgorithms = new List<CompressionAlgorithm>();
        }

        public static List<CompressionAlgorithm> AllAlgorithms
        {
            get;
            protected set;
        }

        public abstract string Name
        {
            get;
        }

        public abstract SymmetricAlgorithm CreateAlgorithm();
    }
}
