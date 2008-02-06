using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using WindowsSshServer.Algorithms;

namespace WindowsSshServer
{
    internal abstract class PublicKeyAlgorithm
    {
        static PublicKeyAlgorithm()
        {
            PublicKeyAlgorithm.AllAlgorithms = new List<PublicKeyAlgorithm>();
            PublicKeyAlgorithm.AllAlgorithms.Add(new SshDss());
            PublicKeyAlgorithm.AllAlgorithms.Add(new SshRsa());
        }

        public static List<PublicKeyAlgorithm> AllAlgorithms
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
