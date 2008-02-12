using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using WindowsSshServer.Algorithms;

using Org.Mentalis.Security.Cryptography;

namespace WindowsSshServer
{
    public class SshClient : IDisposable
    {
        public event EventHandler<EventArgs> Connected;
        public event EventHandler<SshDisconnectedEventArgs> Disconnected;
        public event EventHandler<EventArgs> Error;
        public event EventHandler<SshDebugMessageReceivedEventArgs> DebugMessageReceived;

        protected const uint _maxPacketLength = 35000;   // Maximum total length of packet.

        protected const string _protocolVersion = "2.0"; // Implemented version of SSH protocol.

        protected KexAlgorithm _kexAlgorithm;     // Algorithm to use for kex (key exchange).
        protected PublicKeyAlgorithm
            _hostKeyAlgorithm;                    // Algorithm to use for host key.

        protected EncryptionAlgorithm
            _encryptionAlgorithmCtoS;             // Algorithm to use for encryption of payloads sent from server to client.
        protected EncryptionAlgorithm
            _encryptionAlgorithmStoC;             // Algorithm to use for encryption of payloads sent from client to server.
        protected MacAlgorithm
            _macAlgorithmCtoS;                    // Algorithm to use for computing MACs (Message Authentication Codes) sent from server to client.
        protected MacAlgorithm
            _macAlgorithmStoC;                    // Algorithm to use for computing MACs (Message Authentication Codes) sent from client to server.
        protected CompressionAlgorithm
            _compressionAlgorithmCtoS;            // Algorithm to use for compression of payloads sent from client to server.
        protected CompressionAlgorithm
            _compressionAlgorithmStoC;            // Algorithm to use for compression of payloads sent from server to client.
        protected string _languageCtoS;           // Language to use for message sent from client to server.
        protected string _languageStoC;           // Language to use for message sent from server to client.

        protected EncryptionAlgorithm
            _newEncryptionAlgorithmCtoS;          // New algorithm to use for encryption of payloads sent from server to client.
        protected EncryptionAlgorithm
            _newEncryptionAlgorithmStoC;          // New algorithm to use for encryption of payloads sent from client to server.
        protected MacAlgorithm
            _newMacAlgorithmCtoS;                 // New algorithm to use for computing MACs (Message Authentication Codes) sent from server to client.
        protected MacAlgorithm
            _newMacAlgorithmStoC;                 // New algorithm to use for computing MACs (Message Authentication Codes) sent from client to server.
        protected CompressionAlgorithm
            _newCompressionAlgorithmCtoS;         // New algorithm to use for compression of payloads sent from client to server.
        protected CompressionAlgorithm
            _newCompressionAlgorithmStoC;         // New algorithm to use for compression of payloads sent from server to client.
        protected string _newLanguageCtoS;        // Language to use for message sent from client to server.
        protected string _newLanguageStoC;        // Language to use for message sent from server to client.

        protected RNGCryptoServiceProvider _rng;  // Random number generator.
        protected uint _sendPacketSeqNumber;      // Sequence number of next packet to send.
        protected uint _receivePacketSeqNumber;   // Sequence number of next packet to be received.
        protected string _serverIdString;         // ID string for server.
        protected string _clientIdString;         // ID string for client.
        protected byte[] _serverKexInitPayload;   // Payload of Kex Init message sent by server.
        protected byte[] _clientKexInitPayload;   // Payload of Kex Init message sent by client.
        protected byte[] _exchangeHash;           // Current exchange hash.
        protected byte[] _sessionId;              // Session identifier, which is the first exchange hash.

        protected IConnection _connection;        // Connection to client that provides data stream.
        protected Stream _stream;                 // Stream over which to transmit data.
        protected SshStreamWriter _streamWriter;  // Writes SSH data to stream.
        protected SshStreamReader _streamReader;  // Reads SSH data from stream.
        protected Thread _receiveThread;          // Thread on which to wait for received data.

        private bool _isDisposed = false;         // True if object has been disposed.

        static SshClient()
        {
            // Set default software version.
            SshClient.SoftwareVersion = "WindowsSshServer_"
                + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public static string SoftwareVersion
        {
            get;
            set;
        }

        public SshClient(IConnection connection)
            : this(connection, true)
        {
        }

        public SshClient(Stream stream)
            : this(stream, true)
        {
        }

        public SshClient(IConnection connection, bool addDefaultAlgorithms)
            : this(connection.GetStream(), addDefaultAlgorithms)
        {
            _connection = connection;
        }

        public SshClient(Stream stream, bool addDefaultAlgorithms)
        {
            _stream = stream;

            // Create RNG (random number generator).
            _rng = new RNGCryptoServiceProvider();

            // Initialize properties to default.
            this.ServerComments = null;
            this.Languages = null;

            this.KexAlgorithms = new List<KexAlgorithm>();
            this.HostKeyAlgorithms = new List<PublicKeyAlgorithm>();
            this.EncryptionAlgorithms = new List<EncryptionAlgorithm>();
            this.MacAlgorithms = new List<MacAlgorithm>();
            this.CompressionAlgorithms = new List<CompressionAlgorithm>();

            // Add default algorithms to lists of supported algorithms.
            if (addDefaultAlgorithms) AddDefaultAlgorithms();
        }

        ~SshClient()
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
                    Disconnect();

                    if (_connection != null) _connection.Dispose();
                }

                // Dispose unmanaged resources.
            }

            _isDisposed = true;
        }

        public string ClientProtocolVersion
        {
            get;
            protected set;
        }

        public string ClientSoftwareVersion
        {
            get;
            protected set;
        }

        public string ClientComments
        {
            get;
            protected set;
        }

        public List<KexAlgorithm> KexAlgorithms
        {
            get;
            protected set;
        }

        public List<PublicKeyAlgorithm> HostKeyAlgorithms
        {
            get;
            protected set;
        }

        public List<EncryptionAlgorithm> EncryptionAlgorithms
        {
            get;
            protected set;
        }

        public List<MacAlgorithm> MacAlgorithms
        {
            get;
            protected set;
        }

        public List<CompressionAlgorithm> CompressionAlgorithms
        {
            get;
            protected set;
        }

        public string ServerComments
        {
            get;
            set;
        }

        public IList<string> Languages
        {
            get;
            set;
        }

        public bool IsConnected
        {
            get { return _stream != null; }
        }

        public void ReexchangeKeys()
        {
            // Set all new algorithms to null.
            _newEncryptionAlgorithmCtoS = null;
            _newEncryptionAlgorithmStoC = null;
            _newMacAlgorithmCtoS = null;
            _newMacAlgorithmStoC = null;
            _newCompressionAlgorithmCtoS = null;
            _newCompressionAlgorithmStoC = null;

            // Send kex initialization message.
            SendMsgKexInit();
        }

        public virtual void ConnectionEstablished()
        {
            // Create network objects.
            _streamWriter = new SshStreamWriter(_stream);
            _streamReader = new SshStreamReader(_stream);

            _sendPacketSeqNumber = 0;
            _receivePacketSeqNumber = 0;
            _serverIdString = null;
            _clientIdString = null;
            _sessionId = null;

            // Create thread on which to receive data from connection.
            _receiveThread = new Thread(new ThreadStart(ReceiveData));
            _receiveThread.Start();

            // Client has connected.
            OnConnected(new EventArgs());
        }

        public void Connect()
        {
            //
        }

        public void Disconnect()
        {
            Disconnect(SshDisconnectReason.ByApplication, "");
        }

        protected void Disconnect(SshDisconnectReason reason, string description)
        {
            Disconnect(reason, description, "");
        }

        protected void Disconnect(SshDisconnectReason reason, string description, string language)
        {
            try
            {
                // Send Disconnect message to client.
                SendMsgDisconnect(reason, description, language);
            }
            catch
            {
            }

            // Disconnect from client.
            Disconnect(false);
        }

        protected virtual void Disconnect(bool remotely)
        {
            // Dispose objects for data transmission.
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
            if (_streamWriter != null)
            {
                _streamWriter.Close();
                _streamWriter = null;
            }
            if (_streamReader != null)
            {
                _streamReader.Close();
                _streamReader = null;
            }
            if (_receiveThread != null && Thread.CurrentThread != _receiveThread)
            {
                _receiveThread.Abort();
                _receiveThread = null;
            }

            // Dispose algorithms (current and new).
            DisposeCurrentAlgorithms();

            if (_newEncryptionAlgorithmCtoS != null)
            {
                _newEncryptionAlgorithmCtoS.Dispose();
                _newEncryptionAlgorithmCtoS = null;
            }
            if (_newEncryptionAlgorithmStoC != null)
            {
                _newEncryptionAlgorithmStoC.Dispose();
                _newEncryptionAlgorithmStoC = null;
            }
            if (_newMacAlgorithmCtoS != null)
            {
                _newMacAlgorithmCtoS.Dispose();
                _newMacAlgorithmCtoS = null;
            }
            if (_newMacAlgorithmStoC != null)
            {
                _newMacAlgorithmStoC.Dispose();
                _newMacAlgorithmStoC = null;
            }
            if (_newCompressionAlgorithmCtoS != null)
            {
                _newCompressionAlgorithmCtoS.Dispose();
                _newCompressionAlgorithmCtoS = null;
            }
            if (_newCompressionAlgorithmStoC != null)
            {
                _newCompressionAlgorithmStoC.Dispose();
                _newCompressionAlgorithmStoC = null;
            }

            // Disconnect connection object.
            if (_connection != null) _connection.Disconnect(remotely);

            // Client has disconnected.
            OnDisconnected(new SshDisconnectedEventArgs(remotely));
        }

        protected virtual void AddDefaultAlgorithms()
        {
            // Add default algorithms to lists of supported algorithms.
            this.KexAlgorithms.Add(new SshDiffieHellmanGroup1Sha1());
            this.KexAlgorithms.Add(new SshDiffieHellmanGroup14Sha1());

            this.HostKeyAlgorithms.Add(new SshDss());
            this.HostKeyAlgorithms.Add(new SshRsa());

            this.EncryptionAlgorithms.Add(new SshAes256Cbc());
            this.EncryptionAlgorithms.Add(new SshAes196Cbc());
            this.EncryptionAlgorithms.Add(new SshAes128Cbc());
            this.EncryptionAlgorithms.Add(new SshTripleDesCbc());

            this.MacAlgorithms.Add(new SshHmacSha1());
            this.MacAlgorithms.Add(new SshHmacSha1_96());
            this.MacAlgorithms.Add(new SshHmacMd5());
            this.MacAlgorithms.Add(new SshHmacMd5_96());

            //this.CompressionAlgorithms.Add(new SshZlibCompression());
            this.CompressionAlgorithms.Add(new SshNoCompression());
        }

        protected void DisposeCurrentAlgorithms()
        {
            // Dispose all current algorithms
            if (_kexAlgorithm != null)
            {
                _kexAlgorithm.Dispose();
                _kexAlgorithm = null;
            }
            if (_hostKeyAlgorithm != null)
            {
                _hostKeyAlgorithm.Dispose();
                _hostKeyAlgorithm = null;
            }
            if (_encryptionAlgorithmCtoS != null)
            {
                _encryptionAlgorithmCtoS.Dispose();
                _encryptionAlgorithmCtoS = null;
            }
            if (_encryptionAlgorithmStoC != null)
            {
                _encryptionAlgorithmStoC.Dispose();
                _encryptionAlgorithmStoC = null;
            }
            if (_macAlgorithmCtoS != null)
            {
                _macAlgorithmCtoS.Dispose();
                _macAlgorithmCtoS = null;
            }
            if (_macAlgorithmStoC != null)
            {
                _macAlgorithmStoC.Dispose();
                _macAlgorithmStoC = null;
            }
            if (_compressionAlgorithmCtoS != null)
            {
                _compressionAlgorithmCtoS.Dispose();
                _compressionAlgorithmCtoS = null;
            }
            if (_compressionAlgorithmStoC != null)
            {
                _compressionAlgorithmStoC.Dispose();
                _compressionAlgorithmStoC = null;
            }
        }

        protected void SendMsgKexDhReply(byte[] hostKeyAndCerts, byte[] clientExchangeValue,
            byte[] serverExchangeValue)
        {
            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshMessage.Reply);

                    // Send server public host key and certificates.
                    msgWriter.WriteByteString(hostKeyAndCerts);

                    // Write server kex value.
                    msgWriter.WriteMPint(serverExchangeValue);

                    // Write signature of exchange hash.
                    var signatureData = _hostKeyAlgorithm.CreateSignatureData(_exchangeHash);

                    msgWriter.WriteByteString(signatureData);
                }

                // Send Kex Diffie-Hellman Init message.
                SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgDisconnect(SshDisconnectReason reason, string description, string language)
        {
            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshMessage.Disconnect);

                    // Write reason code.
                    msgWriter.Write((uint)reason);

                    // Write human-readable description of reason for disconnection.
                    msgWriter.WriteByteString(Encoding.UTF8.GetBytes(description));

                    // Write language tag.
                    msgWriter.Write(language);
                }

                // Send Disconnect message.
                SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgUnimplemented()
        {
            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshMessage.Unimplemented);

                    // Send sequence number of unrecognised packet.
                    msgWriter.Write(_receivePacketSeqNumber);
                }

                // Send Unimplemented message.
                SendPacket(msgStream.ToArray());
            }
        }

        protected void SendMsgKexInit()
        {
            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshMessage.KexInit);

                    // Write random cookie.
                    byte[] cookie = new byte[16];

                    _rng.GetBytes(cookie);
                    msgWriter.Write(cookie);

                    // Write list of kex algorithms.
                    msgWriter.WriteNameList((from alg in this.KexAlgorithms select alg.Name).ToArray());

                    // Write list of server host key algorithms.
                    msgWriter.WriteNameList((from alg in this.HostKeyAlgorithms select alg.Name).ToArray());

                    // Write lists of encryption algorithms.
                    msgWriter.WriteNameList((from alg in this.EncryptionAlgorithms select alg.Name).ToArray());
                    msgWriter.WriteNameList((from alg in this.EncryptionAlgorithms select alg.Name).ToArray());

                    // Write lists of MAC algorithms.
                    msgWriter.WriteNameList((from alg in this.MacAlgorithms select alg.Name).ToArray());
                    msgWriter.WriteNameList((from alg in this.MacAlgorithms select alg.Name).ToArray());

                    // Write lists of compression algorithms.
                    msgWriter.WriteNameList((from alg in this.CompressionAlgorithms select alg.Name).ToArray());
                    msgWriter.WriteNameList((from alg in this.CompressionAlgorithms select alg.Name).ToArray());

                    // Write lists of languages.
                    string[] langTags = this.Languages == null ? new string[0]
                        : (from lang in this.Languages select lang).ToArray();

                    msgWriter.WriteNameList(langTags);
                    msgWriter.WriteNameList(langTags);

                    // Write whether first (guessed) kex packet follows.
                    msgWriter.Write(false);

                    // Write reserved values.
                    msgWriter.Write(0u);
                }

                // Send Kex Initialization message.
                _serverKexInitPayload = msgStream.ToArray();
                SendPacket(_serverKexInitPayload);
            }
        }

        protected void SendMsgNewKeys()
        {
            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshMessage.NewKeys);
                }

                // Send New Keys message.
                SendPacket(msgStream.ToArray());
            }
        }

        protected void ReadMsgDisconnect(SshStreamReader msgReader)
        {
            // Read disconnection info.
            SshDisconnectReason reason = (SshDisconnectReason)msgReader.ReadUInt32();
            string description = msgReader.ReadString();
            string languageTag = msgReader.ToString();

            // Disconnect remotely.
            Disconnect(true);
        }

        protected void ReadMsgIgnore(SshStreamReader msgReader)
        {
            // Ignore following data.
            msgReader.ReadString();
        }

        protected void ReadMsgUnimplemented(SshStreamReader msgReader)
        {
            // Read sequence number of packet unrecognised by client. 
            uint packetSeqNumber = msgReader.ReadUInt32();
        }

        protected void ReadMsgDebug(SshStreamReader msgReader)
        {
            // Read debug information.
            bool alwaysDisplay = msgReader.ReadBoolean();
            string message = msgReader.ReadString();
            string languageTag = msgReader.ToString();

            // Write to debug.
            Debug.WriteLine("Debug message");
            Debug.WriteLine("Language: {0}", languageTag);
            Debug.WriteLine(message);
            Debug.WriteLine("");

            // Debug message has been received.
            OnDebugMessageReceived(new SshDebugMessageReceivedEventArgs(alwaysDisplay, message, languageTag));
        }

        protected void ReadMsgServiceRequest(SshStreamReader msgReader)
        {
            throw new IOException();

            //
        }

        protected void ReadMsgServiceAccept(SshStreamReader msgReader)
        {
            //
        }

        protected void ReadMsgDhKexInit(SshStreamReader msgReader)
        {
            // Read client exchange value.
            byte[] clientExchangeValue = msgReader.ReadMPInt();

            // Create server kex value.
            byte[] serverExchangeValue = _kexAlgorithm.CreateKeyExchange();

            // Decrypt shared secret from kex value.
            byte[] sharedSecret = _kexAlgorithm.DecryptKeyExchange(clientExchangeValue);

            // Create data for public host key and certificates.
            byte[] hostKeyAndCerts = _hostKeyAlgorithm.CreateKeyAndCertificatesData();

            // Compute exchange hash.
            _exchangeHash = ComputeExchangeHash(hostKeyAndCerts, clientExchangeValue, serverExchangeValue,
                sharedSecret);

            // Set session identifier if it has not already been set.
            if (_sessionId == null) _sessionId = _exchangeHash;

            // Generate keys from shared secret and exchange hash.
            GenerateKeys(sharedSecret);

            // Send Diffie-Hellman Reply message.
            SendMsgKexDhReply(hostKeyAndCerts, clientExchangeValue, serverExchangeValue);

            // Send New Keys message to start using new keys.
            SendMsgNewKeys();
        }

        protected void ReadMsgKexInit(SshStreamReader msgReader)
        {
            // Read random cookie.
            byte[] cookie = msgReader.ReadBytes(16);

            // Read list of kex algorithms.
            string[] kexAlgorithms = msgReader.ReadNameList();
            string[] serverHostKeyAlgorithms = msgReader.ReadNameList();
            string[] encryptionAlgorithmsCtoS = msgReader.ReadNameList();
            string[] encryptionAlgorithmsStoC = msgReader.ReadNameList();
            string[] macAlgorithmsCtoS = msgReader.ReadNameList();
            string[] macAlgorithmsStoC = msgReader.ReadNameList();
            string[] compAlgorithmsCtoS = msgReader.ReadNameList();
            string[] compAlgorithmsStoC = msgReader.ReadNameList();
            string[] langsCtoS = msgReader.ReadNameList();
            string[] langsStoC = msgReader.ReadNameList();
            bool firstKexPacketFollows = msgReader.ReadBoolean();
            uint reserved0 = msgReader.ReadUInt32();

            // Choose algorithms to use.
            _kexAlgorithm = ChooseKexAlgorithm(kexAlgorithms);
            _hostKeyAlgorithm = ChooseHostKeyAlgorithm(serverHostKeyAlgorithms);

            _newEncryptionAlgorithmCtoS = ChooseEncryptionAlgorithm(encryptionAlgorithmsCtoS);
            _newEncryptionAlgorithmStoC = ChooseEncryptionAlgorithm(encryptionAlgorithmsStoC);
            _newMacAlgorithmCtoS = ChooseMacAlgorithm(macAlgorithmsCtoS);
            _newMacAlgorithmStoC = ChooseMacAlgorithm(macAlgorithmsStoC);
            _newCompressionAlgorithmCtoS = ChooseCompressionAlgorithm(compAlgorithmsCtoS);
            _newCompressionAlgorithmStoC = ChooseCompressionAlgorithm(compAlgorithmsStoC);
            _newLanguageCtoS = ChooseLanguage(langsCtoS);
            _newLanguageStoC = ChooseLanguage(langsStoC);

            // Load host key for chosen algorithm.
            if (_hostKeyAlgorithm is SshDss)
            {
                _hostKeyAlgorithm.ImportKey(@"../../Keys/dss-default.key");
            }
            else if (_hostKeyAlgorithm is SshRsa)
            {
                _hostKeyAlgorithm.ImportKey(@"../../Keys/rsa-default.key");
            }
        }

        protected void ReadMsgNewKeys(SshStreamReader msgReader)
        {
            // Dispose current algorithms.
            DisposeCurrentAlgorithms();

            // Start using new keys and algorithms.
            _encryptionAlgorithmCtoS = _newEncryptionAlgorithmCtoS;
            _encryptionAlgorithmStoC = _newEncryptionAlgorithmStoC;
            _compressionAlgorithmCtoS = _newCompressionAlgorithmCtoS;
            _compressionAlgorithmStoC = _newCompressionAlgorithmStoC;
            _macAlgorithmCtoS = _newMacAlgorithmCtoS;
            _macAlgorithmStoC = _newMacAlgorithmStoC;
            _languageCtoS = _newLanguageCtoS;
            _languageStoC = _newLanguageStoC;
        }

        protected void SendLine(string value)
        {
            // Write chars of line to stream.
            byte[] buffer = Encoding.ASCII.GetBytes(value + "\r\n");

            _streamWriter.Write(buffer);
        }

        protected string ReadLine()
        {
            var lineBuilder = new StringBuilder();

            // Read chars from stream until LF (line feed) is found.
            char curChar;

            do
            {
                curChar = _streamReader.ReadChar();
                lineBuilder.Append(curChar);
            } while (curChar != '\n');

            // Return line without trailing CR LF.
            return lineBuilder.ToString(0, lineBuilder.Length - 2);
        }

        protected void SendPacket(byte[] payload)
        {
            // Calculate block size.
            byte blockSize = (byte)(_encryptionAlgorithmStoC == null ? 8 :
                _encryptionAlgorithmStoC.Algorithm.BlockSize);

            // Calculate random number of extra blocks of padding to add.
            int maxNumExtraPaddingBlocks = (256 - blockSize) / blockSize;
            int numExtraPaddingBlocks = _rng.GetNumber(0, maxNumExtraPaddingBlocks / 2);

            // Calculate packet length information.
            // Packet size (minus MAC) must be multiple of 8 or cipher block size (whichever is larger).
            // Minimum packet size (minus MAC) is 16.
            // Padding length must be between 4 and 255 bytes.
            byte paddingLength = (byte)(blockSize - ((5 + payload.Length) % blockSize)
                + numExtraPaddingBlocks * blockSize);
            if (paddingLength < 4) paddingLength += blockSize;
            uint packetLength = (uint)payload.Length + paddingLength + 1;
            byte[] padding;

            if (packetLength < 12)
            {
                paddingLength += blockSize;
                packetLength += blockSize;
            }

            // Create packet data to send.
            using (var packetStream = new MemoryStream())
            {
                using (var packetWriter = new SshStreamWriter(packetStream))
                {
                    // Write length of packet.
                    packetWriter.Write(packetLength);

                    // Write length of random padding.
                    packetWriter.Write(paddingLength);

                    // Write payload data.
                    packetWriter.Write(payload);

                    // Write bytes of random padding.
                    padding = new byte[paddingLength];
                    _rng.GetBytes(padding);
                    packetWriter.Write(padding);
                }

                // Get packet data.
                var packetData = packetStream.ToArray();

                if (_encryptionAlgorithmStoC != null)
                {
                    // Write encrypted packet data to stream.
                    using (var transform = _encryptionAlgorithmStoC.Algorithm.CreateEncryptor())
                    {
                        var cryptoStream = new CryptoStream(_stream, transform, CryptoStreamMode.Write);

                        // Write packet data to crypto stream.
                        cryptoStream.Write(packetData, 0, packetData.Length);
                        cryptoStream.FlushFinalBlock();
                    }
                }
                else
                {
                    // Write plain packet data to stream.
                    _stream.Write(packetData, 0, packetData.Length);
                }
            }

            // Write to debug.
            SshMessage messageId = (SshMessage)(payload[0]);

            Debug.WriteLine(string.Format("<<< {0}", messageId.ToString()));

            // Write MAC (Message Authentication Code), if MAC algorithm has been agreed on.
            if (_macAlgorithmStoC != null) _streamWriter.Write(ComputeMac(_macAlgorithmStoC,
                _sendPacketSeqNumber, packetLength, paddingLength, payload, padding));

            // Flush stream.
            _stream.Flush();

            // Increment sequence number of next packet to send.
            unchecked { _sendPacketSeqNumber++; }
        }

        protected void ReadPacket()
        {
            Stream cryptoStream = null;
            ICryptoTransform cryptoTransform = null;
            uint packetLength;
            byte paddingLength;
            byte[] payload;
            byte[] mac;

            try
            {
                if (_encryptionAlgorithmCtoS != null)
                {
                    // Packet is encrypted. Use crypto stream for reading.
                    cryptoTransform = _encryptionAlgorithmCtoS.Algorithm.CreateDecryptor();
                    cryptoStream = new CryptoStream(_stream, cryptoTransform, CryptoStreamMode.Read);
                }
                else
                {
                    // Packet is unencrypted. Use normal network stream for reading.
                    cryptoStream = _stream;
                }

                var cryptoStreamReader = new SshStreamReader(cryptoStream);

                // Read packet length information.
                packetLength = cryptoStreamReader.ReadUInt32();
                paddingLength = cryptoStreamReader.ReadByte();

                // Read payload data.
                payload = new byte[packetLength - 1 - paddingLength];

                cryptoStreamReader.Read(payload, 0, payload.Length);

                // Skip bytes of random padding.
                byte[] padding = cryptoStreamReader.ReadBytes(paddingLength);

                // Check if currently using MAC algorithm.
                if (_macAlgorithmCtoS != null)
                {
                    // Read MAC (Message Authentication Code).
                    // MAC is always unencrypted.
                    mac = new byte[_macAlgorithmCtoS.DigestLength];
                    int macBytesRead = ReadCryptoStreamBuffer((CryptoStream)cryptoStream, mac, 0);
                    _streamReader.Read(mac, macBytesRead, mac.Length - macBytesRead);

                    // Verify MAC of received packet.
                    var expectedMac = ComputeMac(_macAlgorithmCtoS, _receivePacketSeqNumber, packetLength,
                        paddingLength, payload, padding);

                    if (!mac.ArrayEquals(expectedMac))
                    {
                        // Invalid MAC.
                        Disconnect(SshDisconnectReason.MacError, string.Format(
                            "Invalid MAC for packet #{0}", _receivePacketSeqNumber));
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
                    Disconnect(SshDisconnectReason.ProtocolError, "Packet length exceeds maximum.");
                    throw new SshDisconnectedException();
                }
            }
            finally
            {
                // Dispose crypto objects.
                if (cryptoTransform != null) cryptoTransform.Dispose();
            }

            // Read received message.
            using (var msgStream = new MemoryStream(payload))
            {
                using (var msgReader = new SshStreamReader(msgStream))
                {
                    try
                    {
                        SshMessage messageId = (SshMessage)msgReader.ReadByte();

                        // Write to debug.
                        Debug.WriteLine(string.Format(">>> {0}", messageId.ToString()));

                        // Check message ID.
                        switch (messageId)
                        {
                            // Standard messages
                            case SshMessage.Disconnect:
                                ReadMsgDisconnect(msgReader);
                                break;
                            case SshMessage.Ignore:
                                ReadMsgIgnore(msgReader);
                                break;
                            case SshMessage.Unimplemented:
                                ReadMsgUnimplemented(msgReader);
                                break;
                            case SshMessage.Debug:
                                ReadMsgDebug(msgReader);
                                break;
                            case SshMessage.ServiceRequest:
                                ReadMsgServiceRequest(msgReader);
                                break;
                            case SshMessage.ServiceAccept:
                                ReadMsgServiceAccept(msgReader);
                                break;
                            case SshMessage.KexInit:
                                // Store payload of message.
                                _clientKexInitPayload = payload;

                                ReadMsgKexInit(msgReader);
                                break;
                            case SshMessage.NewKeys:
                                ReadMsgNewKeys(msgReader);
                                break;
                            // Diffie-Hellman kex messages
                            case SshMessage.Init:
                                ReadMsgDhKexInit(msgReader);
                                break;
                            // Unrecognised message
                            default:
                                // Send Unimplemented message back to client.
                                SendMsgUnimplemented();
                                break;
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        // End of stream here means that the received message was found to be malformed.
                        Disconnect(SshDisconnectReason.ProtocolError, "Bad message format.");
                        throw new SshDisconnectedException();
                    }
                }
            }

            // Increment sequence number of next packet to be received.
            unchecked { _receivePacketSeqNumber++; }
        }

        protected int ReadCryptoStreamBuffer(CryptoStream stream, byte[] buffer, int offset)
        {
            byte[] cryptoStreamBuffer = (byte[])stream.GetType().GetField("_InputBuffer",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stream);
            int cryptoStreamBufferIndex = (int)stream.GetType().GetField("_InputBufferIndex",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stream);
            int bytesRead = cryptoStreamBuffer.Length - cryptoStreamBufferIndex;

            Buffer.BlockCopy(cryptoStreamBuffer, cryptoStreamBufferIndex, buffer, offset, bytesRead);

            return bytesRead;
        }

        protected void GenerateKeys(byte[] sharedSecret)
        {
            // Set initialization vectors for encryption algorithms.
            _newEncryptionAlgorithmCtoS.Algorithm.IV = ComputeEncryptionKey(
                _newEncryptionAlgorithmCtoS.Algorithm.BlockSize / 8, sharedSecret, 'A');
            _newEncryptionAlgorithmStoC.Algorithm.IV = ComputeEncryptionKey(
                _newEncryptionAlgorithmStoC.Algorithm.BlockSize / 8, sharedSecret, 'B');

            // Set keys for encryption algorithms.
            _newEncryptionAlgorithmCtoS.Algorithm.Key = ComputeEncryptionKey(
                _newEncryptionAlgorithmCtoS.Algorithm.KeySize / 8, sharedSecret, 'C');
            _newEncryptionAlgorithmStoC.Algorithm.Key = ComputeEncryptionKey(
                _newEncryptionAlgorithmStoC.Algorithm.KeySize / 8, sharedSecret, 'D');

            // Set keys for MAC algorithms.
            _newMacAlgorithmCtoS.Algorithm.Key = ComputeEncryptionKey(
                _newMacAlgorithmCtoS.Algorithm.HashSize / 8, sharedSecret, 'E');
            _newMacAlgorithmStoC.Algorithm.Key = ComputeEncryptionKey(
                _newMacAlgorithmStoC.Algorithm.HashSize / 8, sharedSecret, 'F');
        }

        protected KexAlgorithm ChooseKexAlgorithm(string[] algorithmNames)
        {
            KexAlgorithm chosenAlg = null;

            // Pick first algorithm in list that satisfies all conditions.
            KexAlgorithm alg;

            foreach (var algName in algorithmNames)
            {
                // Try to find supported algorithm with current name.
                alg = this.KexAlgorithms.SingleOrDefault(item => algName == item.Name);

                if (alg != null)
                {
                    // Set chosen algorithm.
                    chosenAlg = alg;
                    break;
                }
            }

            // Check that algorithm was found.
            if (chosenAlg == null)
            {
                Disconnect(SshDisconnectReason.KeyExchangeFailed,
                    "None of the listed key exchange algorithms are supported.");
                throw new SshDisconnectedException();
            }

            return (KexAlgorithm)chosenAlg.Clone();
        }

        protected PublicKeyAlgorithm ChooseHostKeyAlgorithm(string[] algorithmNames)
        {
            PublicKeyAlgorithm chosenAlg = null;

            // Pick first algorithm in list that satisfies all conditions.
            PublicKeyAlgorithm alg;

            foreach (var algName in algorithmNames)
            {
                // Try to find supported algorithm with current name.
                alg = this.HostKeyAlgorithms.SingleOrDefault(item => algName == item.Name);

                if (alg != null)
                {
                    // Set chosen algorithm.
                    chosenAlg = alg;
                    break;
                }
            }

            // Check that algorithm was found.
            if (chosenAlg == null)
            {
                Disconnect(SshDisconnectReason.KeyExchangeFailed,
                    "None of the listed server host key algorithms are supported.");
                throw new SshDisconnectedException();
            }

            return (PublicKeyAlgorithm)chosenAlg.Clone();
        }

        protected EncryptionAlgorithm ChooseEncryptionAlgorithm(string[] algorithmNames)
        {
            EncryptionAlgorithm chosenAlg = null;

            // Pick first algorithm in list that satisfies all conditions.
            EncryptionAlgorithm alg;

            foreach (var algName in algorithmNames)
            {
                // Try to find supported algorithm with current name.
                alg = this.EncryptionAlgorithms.SingleOrDefault(item => algName == item.Name);

                if (alg != null)
                {
                    // Set chosen algorithm.
                    chosenAlg = alg;
                    break;
                }
            }

            // Check that algorithm was found.
            if (chosenAlg == null)
            {
                Disconnect(SshDisconnectReason.KeyExchangeFailed,
                    "None of the listed encryption algorithms are supported.");
                throw new SshDisconnectedException();
            }

            return (EncryptionAlgorithm)chosenAlg.Clone();
        }

        protected MacAlgorithm ChooseMacAlgorithm(string[] algorithmNames)
        {
            MacAlgorithm chosenAlg = null;

            // Pick first algorithm in list that satisfies all conditions.
            MacAlgorithm alg;

            foreach (var algName in algorithmNames)
            {
                // Try to find supported algorithm with current name.
                alg = this.MacAlgorithms.SingleOrDefault(item => algName == item.Name);

                if (alg != null)
                {
                    // Set chosen algorithm.
                    chosenAlg = alg;
                    break;
                }
            }

            // Check that algorithm was found.
            if (chosenAlg == null)
            {
                Disconnect(SshDisconnectReason.KeyExchangeFailed,
                    "None of the listed MAC algorithms are supported.");
                throw new SshDisconnectedException();
            }

            return (MacAlgorithm)chosenAlg.Clone();
        }

        protected CompressionAlgorithm ChooseCompressionAlgorithm(string[] algorithmNames)
        {
            CompressionAlgorithm chosenAlg = null;

            // Pick first algorithm in list that satisfies all conditions.
            CompressionAlgorithm alg;

            foreach (var algName in algorithmNames)
            {
                // Try to find supported algorithm with current name.
                alg = this.CompressionAlgorithms.SingleOrDefault(item => algName == item.Name);

                if (alg != null)
                {
                    // Set chosen algorithm.
                    chosenAlg = alg;
                    break;
                }
            }

            // Check that algorithm was found.
            if (chosenAlg == null)
            {
                Disconnect(SshDisconnectReason.KeyExchangeFailed,
                    "None of the listed compression algorithms are supported.");
                throw new SshDisconnectedException();
            }

            return (CompressionAlgorithm)chosenAlg.Clone();
        }

        protected string ChooseLanguage(string[] algorithmNames)
        {
            string chosenAlg = null;

            // Pick first algorithm in list that satisfies all conditions.
            string alg;

            foreach (var algName in algorithmNames)
            {
                // Try to find supported algorithm with current name.
                alg = this.Languages.SingleOrDefault(item => algName == item);

                if (alg != null)
                {
                    // Set chosen algorithm.
                    chosenAlg = alg;
                    break;
                }
            }

            return chosenAlg;
        }

        protected byte[] ComputeEncryptionKey(int keySize, byte[] sharedSecret, char letter)
        {
            byte[] keyBuffer = new byte[keySize];
            int keyBufferIndex = 0;
            byte[] currentHash = null;
            int currentHashLength;

            while (keyBufferIndex < keySize)
            {
                using (var hashInputStream = new MemoryStream())
                {
                    using (var hashInputWriter = new SshStreamWriter(hashInputStream))
                    {
                        // Write input data to hash stream.
                        hashInputWriter.WriteMPint(sharedSecret);
                        hashInputWriter.Write(_exchangeHash);

                        if (currentHash == null)
                        {
                            hashInputWriter.Write((byte)letter);
                            hashInputWriter.Write(_sessionId);
                        }
                        else
                        {
                            hashInputWriter.Write(currentHash);
                        }
                    }

                    // Compute hash of input data.
                    currentHash = _kexAlgorithm.ComputeHash(hashInputStream.ToArray());
                }

                // Copy current hash output to key buffer.
                currentHashLength = Math.Min(currentHash.Length, keySize - keyBufferIndex);
                Buffer.BlockCopy(currentHash, 0, keyBuffer, keyBufferIndex, currentHashLength);

                // Advance key buffer index.
                keyBufferIndex += currentHashLength;
            }

            return keyBuffer;
        }

        protected byte[] ComputeExchangeHash(byte[] hostKey, byte[] clientExchangeValue,
            byte[] serverExchangeValue, byte[] sharedSecret)
        {
            using (var hashInputStream = new MemoryStream())
            {
                using (var hashInputWriter = new SshStreamWriter(hashInputStream))
                {
                    // Write kex parameters to stream.
                    hashInputWriter.Write(_clientIdString);
                    hashInputWriter.Write(_serverIdString);
                    hashInputWriter.WriteByteString(_clientKexInitPayload);
                    hashInputWriter.WriteByteString(_serverKexInitPayload);
                    hashInputWriter.WriteByteString(hostKey);
                    hashInputWriter.WriteMPint(clientExchangeValue);
                    hashInputWriter.WriteMPint(serverExchangeValue);
                    hashInputWriter.WriteMPint(sharedSecret);
                }

                // Return hash of input data.
                return _kexAlgorithm.ComputeHash(hashInputStream.ToArray());
            }
        }

        protected byte[] ComputeMac(MacAlgorithm algorithm, uint packetSequenceNumber, uint packetLength,
            byte paddingLength, byte[] payload, byte[] padding)
        {
            using (var macInputStream = new MemoryStream())
            {
                using (var macInputWriter = new SshStreamWriter(macInputStream))
                {
                    // Write sequence number to stream.
                    macInputWriter.Write(packetSequenceNumber);

                    // Write packet data to stream.
                    macInputWriter.Write(packetLength);
                    macInputWriter.Write(paddingLength);
                    macInputWriter.Write(payload);
                    macInputWriter.Write(padding);
                }

                // Return MAC data.
                return algorithm.ComputeHash(macInputStream.ToArray());
            }
        }

        //protected byte[] ComputeMac(MacAlgorithm algorithm, uint packetSequenceNumber,
        //    byte[] unencryptedPacket)
        //{
        //    // Create input data from packet sequence number and unencrypted packet data.
        //    byte[] inputData = new byte[unencryptedPacket.Length + 4];

        //    inputData[0] = (byte)((packetSequenceNumber & 0xFF000000) >> 24);
        //    inputData[1] = (byte)((packetSequenceNumber & 0x00FF0000) >> 16);
        //    inputData[2] = (byte)((packetSequenceNumber & 0x0000FF00) >> 8);
        //    inputData[3] = (byte)(packetSequenceNumber & 0x000000FF);

        //    Buffer.BlockCopy(unencryptedPacket, 0, inputData, 4, unencryptedPacket.Length);

        //    // Return MAC data.
        //    return algorithm.ComputeHash(inputData);
        //}

        protected void FatalErrorOccurred(Exception ex)
        {
            // Disconnect with reason.
            Disconnect(SshDisconnectReason.ProtocolError, string.Format("Fatal error: {0}", ex.Message));

            OnError(new SshErrorEventArgs(ex is SshException ? (SshException)ex
                : new SshException("A fatal error has occurred.", ex)));
        }

        protected virtual void OnConnected(EventArgs e)
        {
            if (Connected != null) Connected(this, new EventArgs());
        }

        protected virtual void OnDisconnected(SshDisconnectedEventArgs e)
        {
            if (Disconnected != null) Disconnected(this, e);
        }

        protected virtual void OnError(SshErrorEventArgs e)
        {
            if (Error != null) Error(this, new EventArgs());
        }

        protected virtual void OnDebugMessageReceived(SshDebugMessageReceivedEventArgs e)
        {
            if (DebugMessageReceived != null) DebugMessageReceived(this, e);
        }

        private void ReceiveData()
        {
            try
            {
                // Create and send server identification string.
                _serverIdString = string.Format("SSH-{0}-{1}", _protocolVersion,
                    SshClient.SoftwareVersion) + (this.ServerComments == null ? "" : " "
                    + this.ServerComments);

                SendLine(_serverIdString);

                // Read identification string of client.
                _clientIdString = ReadLine();
                var clientIdParts = _clientIdString.Split(' ');
                var clientIdVersions = clientIdParts[0].Split(new char[] { '-' }, 3);

                this.ClientProtocolVersion = clientIdVersions[1];
                this.ClientSoftwareVersion = clientIdVersions[2];
                this.ClientComments = clientIdParts.Length > 1 ? clientIdParts[1] : null;

                // Verify that client protocol version is compatible.
                if (this.ClientProtocolVersion != "2.0")
                {
                    Disconnect(SshDisconnectReason.ProtocolVersionNotSupported,
                        "This server only supports SSH v2.0.");
                    throw new SshDisconnectedException();
                }

                // Send kex initialization message.
                SendMsgKexInit();

                // Read packets from network stream until end of stream is reached.
                while (true)
                {
                    // Read next packet.
                    ReadPacket();
                }
            }
            catch (SshDisconnectedException)
            {
                // Ignore.
            }
            catch (EndOfStreamException)
            {
                // Client disconnected.
                Disconnect(true);
            }
            catch (IOException exIo)
            {
                // Check if socket exception was root cause.
                if (exIo.InnerException is SocketException)
                {
                    var exSocket = (SocketException)exIo.InnerException;

                    if (exSocket.SocketErrorCode == SocketError.ConnectionAborted)
                        Disconnect(false);
                    else if (exSocket.SocketErrorCode == SocketError.ConnectionReset)
                        Disconnect(true);
                    else
                        // Fatal error has occurred.
                        FatalErrorOccurred(exSocket);
                }
                else
                {
                    // Fatal error has occurred.
                    FatalErrorOccurred(exIo);
                }
            }
            catch (SshException exSsh)
            {
                // Fatal error has occurred.
                FatalErrorOccurred(exSsh);
            }
            catch (ThreadAbortException)
            {
                // Thread has been aborted.
            }
            catch (Exception ex)
            {
                // Fatal error has occurred.
                FatalErrorOccurred(ex);
            }
            finally
            {
            }
        }
    }

    public class SshDisconnectedEventArgs : EventArgs
    {
        public SshDisconnectedEventArgs(bool disconnectedRemotely)
        {
            this.DisconnectedRemotely = disconnectedRemotely;
        }

        public bool DisconnectedRemotely
        {
            get;
            protected set;
        }
    }

    public class SshErrorEventArgs : EventArgs
    {
        public SshErrorEventArgs(SshException exception)
        {
            this.Exception = exception;
        }

        public SshException Exception
        {
            get;
            protected set;
        }
    }

    public class SshDebugMessageReceivedEventArgs : EventArgs
    {
        public SshDebugMessageReceivedEventArgs(bool alwaysDisplay, string message, string language)
        {
            this.AlwaysDisplay = alwaysDisplay;
            this.Message = message;
            this.Language = language;
        }

        public bool AlwaysDisplay
        {
            get;
            protected set;
        }

        public string Message
        {
            get;
            protected set;
        }

        public string Language
        {
            get;
            protected set;
        }
    }

    public enum SshDisconnectReason : uint
    {
        HostNotAllowedToConnect = 1,
        ProtocolError = 2,
        KeyExchangeFailed = 3,
        Reserved = 4,
        MacError = 5,
        CompressionError = 6,
        ServiceNotAvailable = 7,
        ProtocolVersionNotSupported = 8,
        HostKeyNotVerifiable = 9,
        ConnectionLost = 10,
        ByApplication = 11,
        TooManyConnections = 12,
        AuthCancelledByUser = 13,
        NoMoreAuthMethodsAvailable = 14,
        IllegalUserName = 15
    }

    public enum SshMessage : byte
    {
        Disconnect = 1,
        Ignore = 2,
        Unimplemented = 3,
        Debug = 4,
        ServiceRequest = 5,
        ServiceAccept = 6,
        KexInit = 20,
        NewKeys = 21,
        Init = 30,
        Reply = 31
    }
}
