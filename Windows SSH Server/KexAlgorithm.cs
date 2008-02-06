using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using WindowsSshServer.Algorithms;

namespace WindowsSshServer
{
    internal abstract class KexAlgorithm
    {
        static KexAlgorithm()
        {
            KexAlgorithm.AllAlgorithms = new List<KexAlgorithm>();
            KexAlgorithm.AllAlgorithms.Add(new SshDiffieHellmanGroup1Sha1());
            KexAlgorithm.AllAlgorithms.Add(new SshDiffieHellmanGroup14Sha1());
        }

        public static List<KexAlgorithm> AllAlgorithms
        {
            get;
            protected set;
        }

        public abstract string Name
        {
            get;
        }

        public abstract AsymmetricAlgorithm CreateAlgorithm();
    }
}
