using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SshDotNet.Algorithms
{
    public class SshDss : PublicKeyAlgorithm
    {
        protected new DSACryptoServiceProvider _algorithm; // Algorithm to use.

        public SshDss()
            : base()
        {
            _algorithm = new DSACryptoServiceProvider();
        }

        public new DSACryptoServiceProvider Algorithm
        {
            get { return _algorithm; }
        }

        public override string Name
        {
            get { return "ssh-dss"; }
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

        public override byte[] CreateKeyAndCertificatesData()
        {
            using (var dataStream = new MemoryStream())
            {
                using (var dataWriter = new SshStreamWriter(dataStream))
                {
                    // Export parameters for algorithm key.
                    var algParams = _algorithm.ExportParameters(true);

                    // Write data to stream.
                    dataWriter.Write(this.Name);
                    dataWriter.WriteMPint(algParams.P);
                    dataWriter.WriteMPint(algParams.Q);
                    dataWriter.WriteMPint(algParams.G);
                    dataWriter.WriteMPint(algParams.Y);
                }

                return dataStream.ToArray();
            }
        }

        public override byte[] SignHash(byte[] hashData)
        {
            var hashAlgOid = CryptoConfig.MapNameToOID("SHA1");

            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                return _algorithm.SignHash(sha1.ComputeHash(hashData), hashAlgOid);
            }
        }

        public override object Clone()
        {
            return new SshDss();
        }
    }
}
