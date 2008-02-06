using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using WindowsSshServer.Algorithms;

namespace WindowsSshServer
{
    internal abstract class MacAlgorithm
    {
        static MacAlgorithm()
        {
            MacAlgorithm.AllAlgorithms = new List<MacAlgorithm>();
            MacAlgorithm.AllAlgorithms.Add(new SshHmacSha1());
            MacAlgorithm.AllAlgorithms.Add(new SshHmacSha1_96());
            MacAlgorithm.AllAlgorithms.Add(new SshHmacMd5());
            MacAlgorithm.AllAlgorithms.Add(new SshHmacMd5_96());
        }

        public static List<MacAlgorithm> AllAlgorithms
        {
            get;
            protected set;
        }

        public abstract string Name
        {
            get;
        }

        public abstract HMAC CreateAlgorithm();
    }
}
