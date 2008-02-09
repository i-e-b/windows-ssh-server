using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer.Algorithms
{
    internal class SshRsa : PublicKeyAlgorithm
    {
        protected RSACryptoServiceProvider _algorithm; // Algorithm to use.

        internal SshRsa()
            : base()
        {
            _algorithm = new RSACryptoServiceProvider();
        }

        public override string Name
        {
            get { return "ssh-rsa"; }
        }

        public RSACryptoServiceProvider Algorithm
        {
            get { return _algorithm; }
        }

        public override void ImportKey(Stream stream)
        {
            // Read XML for key from stream.
            using (var reader = new StreamReader(stream))
            {
                _algorithm.FromXmlString(reader.ReadToEnd());
            }
        }

        public override void ExportKey(Stream stream)
        {
            // Write XML for key to stream.
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(_algorithm.ToXmlString(true));
            }
        }

        public override byte[] CreateKeyData()
        {
            using (var dataStream = new MemoryStream())
            {
                using (var dataWriter = new SshStreamWriter(dataStream))
                {
                    // Export parameters for algorithm key.
                    var algParams = _algorithm.ExportParameters(true);

                    // Write parameters to stream.
                    dataWriter.Write("ssh-dss");
                    dataWriter.WriteMPint(algParams.Exponent);
                    dataWriter.WriteMPint(algParams.Modulus);
                }

                return dataStream.GetBuffer();
            }
        }

        public override byte[] SignHash(byte[] hashData)
        {
            var hashAlgOid = CryptoConfig.MapNameToOID("SHA1");

            return _algorithm.SignHash(hashData, hashAlgOid);
        }
    }
}
