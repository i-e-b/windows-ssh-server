using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    internal abstract class MacAlgorithm
    {
        protected HMAC _algorithm; // Algorithm to use.

        public abstract string Name
        {
            get;
        }

        public HMAC Algorithm
        {
            get { return _algorithm; }
        }

        public virtual byte[] ComputeHash(byte[] input)
        {
            return _algorithm.ComputeHash(input);
        }
    }
}
