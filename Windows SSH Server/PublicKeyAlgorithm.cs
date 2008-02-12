using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    public abstract class PublicKeyAlgorithm : IDisposable, ICloneable
    {
        protected AsymmetricAlgorithm _algorithm; // Algorithm to use.

        private bool _isDisposed = false;         // True if object has been disposed.

        public PublicKeyAlgorithm()
        {
        }

        ~PublicKeyAlgorithm()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    if (_algorithm != null) _algorithm.Clear();
                }

                // Dispose unmanaged resources.
            }

            _isDisposed = true;
        }

        public abstract string Name
        {
            get;
        }

        public AsymmetricAlgorithm Algorithm
        {
            get { return _algorithm; }
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

        public abstract byte[] CreateKeyAndCertificatesData();

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

                return dataStream.ToArray();
            }
        }

        public abstract byte[] SignHash(byte[] hashData);

        public abstract object Clone();
    }
}
