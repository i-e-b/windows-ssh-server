using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsSshServer
{
    public sealed class SshPacket
    {
        private uint _packetLength;  // Length of packet, excluding MAC, in bytes.
        private byte _paddingLength; // Length of random padding, in bytes.
        private byte[] _payload;     // Payload data (unencrypted and uncompressed).
        private byte[] _mac;         // MAC (Message Authentication Code).

        public SshPacket(Stream stream, EncryptionAlgorithm encryptionAlg,
            CompressionAlgorithm compressionAlg, MacAlgorithm macAlg)
            : this()
        {
            Initialize(stream, encryptionAlg, compressionAlg, macAlg);
        }

        public SshPacket()
        {
        }

        public byte[] ToByteArray()
        {
            return ToByteArray(null, null, null);
        }

        public byte[] ToByteArray(EncryptionAlgorithm encryptionAlg, CompressionAlgorithm compressionAlg,
            MacAlgorithm macAlg)
        {
            using (var packetStream = new MemoryStream())
            {
                // Write packet to memory stream.
                WriteTo(packetStream, encryptionAlg, compressionAlg, macAlg);

                // Return packet data.
                return packetStream.ToArray();
            }
        }

        public void WriteTo(Stream stream)
        {
            WriteTo(stream, null, null, null);
        }

        public void WriteTo(Stream stream, EncryptionAlgorithm encryptionAlg,
            CompressionAlgorithm compressionAlg, MacAlgorithm macAlg)
        {
            //
        }

        private void Initialize(Stream stream, EncryptionAlgorithm encryptionAlg,
            CompressionAlgorithm compressionAlg, MacAlgorithm macAlg)
        {
            Stream networkCryptoStream = null;
            ICryptoTransform cryptoTransform = null;
            
            try
            {
                if (encryptionAlg != null)
                {
                    // Packet is encrypted. Use crypto stream for reading.
                    cryptoTransform = encryptionAlg.Algorithm.CreateDecryptor();

                    networkCryptoStream = new CryptoStream(stream, cryptoTransform,
                        CryptoStreamMode.Read);
                }
                else
                {
                    // Packet is unencrypted. Use normal network stream for reading.
                    networkCryptoStream = stream;
                }

                var cryptoStreamReader = new SshStreamReader(networkCryptoStream);

                // Read packet information.
                _packetLength = cryptoStreamReader.ReadUInt32();
                _paddingLength = cryptoStreamReader.ReadByte();

                // Read payload data.
                payload = new byte[_packetLength - 1 - _paddingLength];

                cryptoStreamReader.Read(_payload, 0, _payload.Length);

                // Skip bytes of random padding.
                cryptoStreamReader.ReadBytes(_paddingLength);

                // Check if currently using MAC algorithm.
                if (macAlg != null)
                {
                    // Read MAC (Message Authentication Code).
                    // MAC is always unencrypted.
                    _mac = new byte[macAlg.DigestLength];

                    _streamReader.Read(_mac, 0, _mac.Length);

                    // Verify MAC of received packet.
                    var expectedMac = ComputeMac(macAlg, _receivePacketSeqNumber, payload);

                    if (!mac.Equals(expectedMac))
                    {
                        // Invalid MAC.
                        Disconnect(SshDisconnectReason.MacError,
                            string.Format("Invalid MAC for packet #{0}", _receivePacketSeqNumber));
                        throw new SshDisconnectedException();
                    }
                }
                else
                {
                    // Set MAC to empty array.
                    mac = new byte[0];
                }

                // Check that packet length does not exceed maximum.
                if (4 + packetLength + mac.Length > _maxPacketLength)
                {
                    Disconnect(SshDisconnectReason.ProtocolError,
                        "Packet length exceeds maximum.");
                    throw new SshDisconnectedException();
                }
            }
            finally
            {
                // Dispose crypto resources.
                if (cryptoTransform != null) cryptoTransform.Dispose();
            }
        }

        private byte[] ComputeMac(MacAlgorithm algorithm, uint packetSequenceNumber,
            byte[] unencryptedPacket)
        {
            // Create input data from packet sequency number and unencrypted packet data.
            byte[] inputData = new byte[unencryptedPacket.Length + 4];

            inputData[0] = (byte)((packetSequenceNumber & 0xFF000000) >> 24);
            inputData[1] = (byte)((packetSequenceNumber & 0x00FF0000) >> 16);
            inputData[2] = (byte)((packetSequenceNumber & 0x0000FF00) >> 8);
            inputData[3] = (byte)(packetSequenceNumber & 0x000000FF);

            Buffer.BlockCopy(unencryptedPacket, 0, inputData, 4, unencryptedPacket.Length);

            // Return MAC data.
            return algorithm.ComputeHash(inputData);
        }
    }
}
