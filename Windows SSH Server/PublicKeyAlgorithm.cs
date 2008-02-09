using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    public abstract class PublicKeyAlgorithm
    {
        public abstract string Name
        {
            get;
        }

        public void ImportKey(string fileName)
        {
            // Open file stream.
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                ImportKey(fileStream);
            }
        }

        public abstract void ImportKey(Stream stream);

        public void ExportKey(string fileName)
        {
            // Open file stream.
            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                ExportKey(fileStream);
            }
        }

        public abstract void ExportKey(Stream stream);

        public abstract byte[] CreateKeyData();

        public byte[] CreateSignatureData(byte[] hashData)
        {
            using (var dataStream = new MemoryStream())
            {
                using (var dataWriter = new SshStreamWriter(dataStream))
                {
                    // Create signature of hash data.
                    var signature = SignHash(hashData);

                    // Write data to stream.
                    dataWriter.Write(this.Name);
                    dataWriter.WriteByteString(signature);
                }

                return dataStream.GetBuffer();
            }
        }

        public abstract byte[] SignHash(byte[] hashData);
    }
}
