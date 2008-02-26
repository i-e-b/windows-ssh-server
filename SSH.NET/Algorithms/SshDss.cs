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
            base._algorithm = _algorithm;
        }

        public new DSACryptoServiceProvider Algorithm
        {
            get { return _algorithm; }
        }

        public override string Name
        {
            get { return "ssh-dss"; }
        }

        public override void LoadKeyAndCertificatesData(byte[] data)
        {
            using (var dataStream = new MemoryStream(data))
            {
                using (var dataReader = new SshStreamReader(dataStream))
                {
                    // Read parameters from stream.
                    var algParams = new DSAParameters();

                    if (dataReader.ReadString() != this.Name) throw new CryptographicException(
                       "Key and certificates were not created with this algorithm.");
                    algParams.P = dataReader.ReadMPInt();
                    algParams.Q = dataReader.ReadMPInt();
                    algParams.G = dataReader.ReadMPInt();
                    algParams.Y = dataReader.ReadMPInt();

                    // Import parameters for algorithm key.
                    _algorithm.ImportParameters(algParams);
                }
            }
        }

        public override byte[] CreateKeyAndCertificatesData()
        {
            using (var dataStream = new MemoryStream())
            {
                using (var dataWriter = new SshStreamWriter(dataStream))
                {
                    // Export parameters for algorithm key.
                    var algParams = _algorithm.ExportParameters(false);

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

        public override bool VerifyData(byte[] data, byte[] signature)
        {
            return _algorithm.VerifyData(data, signature);
        }

        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            var hashAlgOid = CryptoConfig.MapNameToOID("SHA1");

            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                return _algorithm.VerifyHash(hash, hashAlgOid, signature);
            }
        }

        public override byte[] SignData(byte[] data)
        {
            return _algorithm.SignData(data);
        }

        public override byte[] SignHash(byte[] hash)
        {
            var hashAlgOid = CryptoConfig.MapNameToOID("SHA1");

            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                return _algorithm.SignHash(sha1.ComputeHash(hash), hashAlgOid);
            }
        }

        public override object Clone()
        {
            return new SshDss();
        }
    }
}
