using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace SshDotNet
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

        #region Key Importing/Exporting

        public void ImportPublicKey(SshPublicKey key)
        {
            // Import only public key parameters.
            LoadKeyAndCertificatesData(key.KeyAndCertificatesData);
        }

        public SshPublicKey ExportPublicKey()
        {
            // Export only public key parameters.
            return new SshPublicKey()
            {
                KeyAndCertificatesData = CreateKeyAndCertificatesData()
            };
        }

        public void ImportKey(Stream stream)
        {
            // Import key parameters from stream in XML format.
            using (var xmlReader = new XmlTextReader(stream))
            {
                xmlReader.MoveToContent();
                ImportKey(xmlReader.ReadOuterXml());
            }
        }

        public void ImportKey(string xml)
        {
            // Import key parameters from XML string.
            _algorithm.FromXmlString(xml);
        }

        public void ExportKey(Stream stream)
        {
            // Write key parameters to stream in XML format.
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(ExportKey());
            }
        }

        public string ExportKey()
        {
            // Export key parameters to XML string.
            return _algorithm.ToXmlString(true);
        }

        #endregion

        public abstract void LoadKeyAndCertificatesData(byte[] data);

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
