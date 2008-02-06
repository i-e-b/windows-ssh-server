using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using WindowsSshServer.Algorithms;

namespace WindowsSshServer
{
    internal abstract class EncryptionAlgorithm
    {
        static EncryptionAlgorithm()
        {
            EncryptionAlgorithm.AllAlgorithms = new List<EncryptionAlgorithm>();
            EncryptionAlgorithm.AllAlgorithms.Add(new SshTripleDesCbc());
            EncryptionAlgorithm.AllAlgorithms.Add(new SshAes128Cbc());
            EncryptionAlgorithm.AllAlgorithms.Add(new SshAes196Cbc());
        }

        public static List<EncryptionAlgorithm> AllAlgorithms
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
