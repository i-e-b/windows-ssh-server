using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    public static class CryptoExtensions
    {
        public static byte[] Encrypt(this SymmetricAlgorithm algorithm, byte[] input)
        {
            // Create memory stream to which to write encrypted data.
            using (var memoryStream = new MemoryStream())
            {
                //// Ensure that crypto operations are thread-safe.
                //lock (algorithm)
                //{
                    using (var transform = algorithm.CreateEncryptor())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, transform,
                            CryptoStreamMode.Write))
                        {
                            // Write input data to crypto stream.
                            cryptoStream.Write(input, 0, input.Length);
                            cryptoStream.FlushFinalBlock();

                            // Return encrypted data.
                            return memoryStream.ToArray();
                        }
                    }
                //}
            }
        }

        public static byte[] Decrypt(this SymmetricAlgorithm algorithm, byte[] input)
        {
            // Create memory stream to which from read decrypted data.
            using (var memoryStream = new MemoryStream(input))
            {
                //// Ensure that crypto operations are thread-safe.
                //lock (algorithm)
                //{
                    using (var transform = algorithm.CreateDecryptor())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, transform,
                            CryptoStreamMode.Read))
                        {
                            // Read output data to crypto stream.
                            byte[] output = new byte[input.Length];
                            int outputLength = cryptoStream.Read(output, 0, output.Length);

                            Array.Resize(ref output, outputLength);

                            // Return decrypted data.
                            return output;
                        }
                    }
                //}
            }
        }
    }
}
