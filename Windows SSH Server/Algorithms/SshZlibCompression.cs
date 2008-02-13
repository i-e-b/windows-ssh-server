using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using DotZLib;

namespace WindowsSshServer.Algorithms
{
    public class SshZlibCompression : CompressionAlgorithm
    {
        protected Deflater _compressor;       // Compresses data.
        protected Inflater _decompressor;     // Decompresses data.
        protected MemoryStream _outputStream; // Stream to which to output data.

        private bool _isDisposed = false;     // True if object has been disposed.

        public SshZlibCompression()
            : base()
        {
            _compressor = new Deflater(CompressLevel.Default);
            _compressor.DataAvailable += new DataAvailableHandler(_compressor_DataAvailable);

            _decompressor = new Inflater();
            _decompressor.DataAvailable += new DataAvailableHandler(_decompressor_DataAvailable);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        // Dispose managed resources.
                        if (_compressor != null) _compressor.Dispose();
                        if (_decompressor != null) _decompressor.Dispose();
                    }

                    // Dispose unmanaged resources.
                }

                _isDisposed = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override string Name
        {
            get { return "zlib"; }
        }

        public override byte[] Compress(byte[] input)
        {
            lock (_compressor)
            {
                using (_outputStream = new MemoryStream())
                {
                    // Write input data to compressor.
                    _compressor.Add(input);
                    _compressor.PartialFlush();

                    // Return compressed data.
                    return _outputStream.ToArray();
                }
            }
        }

        public override byte[] Decompress(byte[] input)
        {
            lock (_decompressor)
            {
                using (_outputStream = new MemoryStream())
                {
                    // Write input data to decompressor.
                    _decompressor.Add(input);
                    _decompressor.PartialFlush();

                    // Return decompressed data.
                    return _outputStream.ToArray();
                }
            }
        }

        public override object Clone()
        {
            return new SshZlibCompression();
        }

        private void _compressor_DataAvailable(byte[] data, int startIndex, int count)
        {
            lock (_compressor)
            {
                // Write compressed data to output stream.
                _outputStream.Write(data, startIndex, count);
            }
        }

        private void _decompressor_DataAvailable(byte[] data, int startIndex, int count)
        {
            lock (_decompressor)
            {
                // Write decompressed data to output stream.
                _outputStream.Write(data, startIndex, count);
            }
        }
    }
}
