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
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<EventArgs> Error;
        public event EventHandler<EventArgs> DebugMessage;

        protected const string _protocolVersion = "2.0"; // Implemented version of SSH protocol.

        protected RNGCryptoServiceProvider _rng; // Random number generator.
        protected KexAlgorithm _kexAlgorithm;    // Algorithm to use for kex (key exchange).
        protected PublicKeyAlgorithm
            _hostKeyAlgorithm;                   // Algorithm to use for host key.
        protected EncryptionAlgorithm
            _encryptionAlgorithm;                // Algorithm to use for encryption of payloads.
        protected CompressionAlgorithm
            _compressionAlgorithm;               // Algorithm to use for compression of payloads.
        protected MacAlgorithm _macAlgorithm;    // Algorithm to use for computing MAC (Message Authentication Code).

        protected uint _sendPacketSeqNumber;     // Sequence number of next packet to send.
        protected uint _receivePacketSeqNumber;  // Sequence number of next packet to be received.
        protected string _serverIdString;        // ID string for server.
        protected string _clientIdString;        // ID string for client.
        protected byte[] _serverKexInitPayload;  // Payload of Kex Init message sent by server.
        protected byte[] _clientKexInitPayload;  // Payload of Kex Init message sent by client.
        protected byte[] _exchangeHash;          // Current exchange hash.
        protected byte[] _sessionId;             // Session identifier, which is the first exchange hash.

        protected TcpClient _tcpClient;          // Client TCP connection.
        protected NetworkStream _networkStream;  // Stream for transmitting data across connection.
        protected SshStreamWriter _streamWriter; // Writes data to network stream.
        protected SshStreamReader _streamReader; // Reads data from network stream.
        protected Thread _receiveThread;         // Thread on which to wait for received data.

        private bool _isDisposed = false;        // True if object has been disposed.

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

        public SshClient(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;

            // Create cryptographic objects.
            _rng = new RNGCryptoServiceProvider();

            // Initialize properties to default.
            this.ServerComments = null;
            this.Languages = null;

            this.KexAlgorithms = new List<KexAlgorithm>();
            this.HostKeyAlgorithms = new List<PublicKeyAlgorithm>();
            this.MacAlgorithms = new List<MacAlgorithm>();
            this.EncryptionAlgorithms = new List<EncryptionAlgorithm>();
            this.CompressionAlgorithms = new List<CompressionAlgorithm>();

            AddDefaultAlgorithms();

            // Notify that client has connected.
            OnConnected();
        }

        ~SshClient()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    Disconnect();

                    _kexAlgorithm = null;
                    _hostKeyAlgorithm = null;
                    _encryptionAlgorithm = null;
                    _compressionAlgorithm = null;
                    _macAlgorithm = null;

                    GC.Collect();
                }

                // Dispose unmanaged resources.
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        public IList<SshLanguage> Languages
        {
            get;
            set;
        }

        public bool IsConnected
        {
            get { return _tcpClient != null; }
        }

        public void Connect()
        {
            //
        }

        public void Disconnect()
        {
            Disconnect(SshDisconnectReason.ByApplication, null);
        }

        protected void Disconnect(SshDisconnectReason reason, string description)
        {
            Disconnect(reason, description, SshLanguage.None);
        }

        protected void Disconnect(SshDisconnectReason reason, string description, SshLanguage language)
        {
            try
            {
                // Send Disconnect message to client.
                SendMsgDisconnect(reason, description, language);
            }
            catch { }

            // Disconnect from client.
            Disconnect(false);
        }

        protected virtual void Disconnect(bool remotely)
        {
            if (_tcpClient == null) return;

            // Close connection objects.
            _tcpClient.Close();
            _tcpClient = null;

            if (_networkStream != null) _networkStream.Dispose();
            if (_streamWriter != null) _streamWriter.Close();
            if (_streamReader != null) _streamReader.Close();
            if (_receiveThread != null && Thread.CurrentThread != _receiveThread) _receiveThread.Abort();

            GC.Collect();

            OnDisconnected(remotely);
        }

        protected virtual void AddDefaultAlgorithms()
        {
            // Add default algorithms to lists for this client.
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

            this.CompressionAlgorithms.Add(new SshZlibCompression());
            this.CompressionAlgorithms.Add(new SshNoCompression());
        }

        protected void SendMsgKexDhReply(byte[] clientExchangeValue)
        {
            // Create server kex value.
            byte[] serverExchangeValue = _kexAlgorithm.CreateKeyExchange();

            // Decrypt shared secret from kex value.
            byte[] sharedSecret = _kexAlgorithm.DecryptKeyExchange(clientExchangeValue);

            // Get public host key and certificates.
            byte[] hostKey = _hostKeyAlgorithm.CreateKeyData();
            
            // Compute exchange hash.
            _exchangeHash = ComputeExchangeHash(hostKey, clientExchangeValue, serverExchangeValue,
                sharedSecret);

            // Set session identifier if it has not already been set.
            if (_sessionId == null) _sessionId = _exchangeHash;

            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
                    // Write message ID.
                    msgWriter.Write((byte)SshMessageKexDiffieHellman.Reply);

                    // Send server public host key and certificates.
                    msgWriter.WriteByteString(hostKey);

                    // Write server kex value.
                    msgWriter.WriteMPint(serverExchangeValue);
                    
                    // Write signature of exchange hash.
                    var signatureData = _hostKeyAlgorithm.CreateSignatureData(_exchangeHash);

                    msgWriter.WriteByteString(signatureData);
                }

                // Send Kex Diffie-Hellman Init message.
                SendPacket(msgStream.GetBuffer());
            }
        }

        protected void SendMsgDisconnect(SshDisconnectReason reason, string description,
            SshLanguage language)
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
                    msgWriter.Write(Encoding.UTF8.GetBytes(description));

                    // Write language tag.
                    msgWriter.Write(language.Tag);
                }

                // Send Disconnect message.
                SendPacket(msgStream.GetBuffer());
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
                SendPacket(msgStream.GetBuffer());
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
                        : (from lang in this.Languages select lang.Tag).ToArray();

                    msgWriter.WriteNameList(langTags);
                    msgWriter.WriteNameList(langTags);

                    // Write whether first (guessed) kex packet follows.
                    msgWriter.Write(false);

                    // Write reserved values.
                    msgWriter.Write(0u);
                }

                // Send Kex Initialization message.
                _serverKexInitPayload = msgStream.GetBuffer();
                SendPacket(_serverKexInitPayload);
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
        }

        protected void ReadMsgServiceRequest(SshStreamReader msgReader)
        {
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

            // Send reply message.
            SendMsgKexDhReply(clientExchangeValue);
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

            // Pick kex algorithm to use (see RFC4253 section 7.1).
            KexAlgorithm kexAlg = null;

            foreach (var kexAlgName in kexAlgorithms)
            {
                // Try to find supported algorithm with current name.
                kexAlg = this.KexAlgorithms.SingleOrDefault(item => kexAlgName == item.Name);

                if (kexAlg != null)
                {
                    // Set kex algorithm to use.
                    _kexAlgorithm = kexAlg;
                }
            }

            // Check that kex algorithm was found.
            if (_kexAlgorithm == null)
            {
                Disconnect(SshDisconnectReason.KeyExchangeFailed,
                    "None of the key exchange algorithms listed are supported.");
            }

            // Pick server host key algorithm to use (see RFC4253 section 7.1).
            PublicKeyAlgorithm hostKeyAlg = null;

            foreach (var hostKeyAlgName in serverHostKeyAlgorithms)
            {
                // Try to find supported algorithm with current name.
                hostKeyAlg = this.HostKeyAlgorithms.SingleOrDefault(item => hostKeyAlgName == item.Name);

                if (hostKeyAlg != null)
                {
                    // Set host key algorithm to use.
                    _hostKeyAlgorithm = hostKeyAlg;
                }
            }

            // Check that host key algorithm was found.
            if (_hostKeyAlgorithm == null)
            {
                Disconnect(SshDisconnectReason.KeyExchangeFailed,
                    "None of the server host key algorithms listed are supported.");
            }

            // Load key for host algorithm.
            if (_hostKeyAlgorithm is SshDss)
            {
                SshDss hostKeyAlgDss = (SshDss)_hostKeyAlgorithm;

                hostKeyAlg.ImportKey(@"../../Keys/dss-default.key");
            }
            else if (_hostKeyAlgorithm is SshRsa)
            {
                SshRsa hostKeyAlgRsa = (SshRsa)_hostKeyAlgorithm;

                hostKeyAlg.ImportKey(@"../../Keys/rsa-default.key");
            }
        }

        protected void ReadMsgNewKeys(SshStreamReader msgReader)
        {
            // Start using new keys and algorithms.
            //
        }

        public void SendLine(string value)
        {
            // Write chars of line to stream.
            byte[] buffer = Encoding.ASCII.GetBytes(value + "\r\n");

            _streamWriter.Write(buffer);
        }

        public string ReadLine()
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
            // Calculate packet length information.
            byte paddingLength = (byte)(8 - ((5 + payload.Length) % 8));
            if (paddingLength < 4) paddingLength += 8;
            uint packetLength = (uint)payload.Length + paddingLength + 1;

            // Create packet data.
            byte[] packetData;

            using (var packetStream = new MemoryStream())
            {
                using (var packetWriter = new SshStreamWriter(packetStream))
                {
                    // Write length of packet.
                    _streamWriter.Write(packetLength);

                    // Write length of random padding.
                    _streamWriter.Write(paddingLength);

                    // Write payload data.
                    _streamWriter.Write(payload);

                    // Write bytes of random padding.
                    byte[] padding = new byte[paddingLength];

                    _rng.GetBytes(padding);
                    _streamWriter.Write(padding);
                }

                packetData = packetStream.GetBuffer();
            }

            // Write packet data to network stream.
            _streamWriter.Write(packetData);

            // Write to debug.
            SshMessage messageId = (SshMessage)(payload[0]);

            Debug.WriteLine(string.Format("<<< {0}", messageId.ToString()));

            // Write MAC (Message Authentication Code), if MAC algorithm has been agreed on.
            if (_macAlgorithm != null) _streamWriter.Write(ComputeMac(packetData));

            // Increment sequence number of next packet to send.
            unchecked { _sendPacketSeqNumber++; }
        }

        protected void ReadPacket()
        {
            // Read packet length information.
            uint packetLength = _streamReader.ReadUInt32();
            byte paddingLength = _streamReader.ReadByte();

            // Read payload data.
            byte[] rawPayload = new byte[packetLength - 1 - paddingLength];

            _streamReader.Read(rawPayload, 0, rawPayload.Length);

            // Skip bytes of random padding.
            _streamReader.ReadBytes(paddingLength);

            // Read MAC (Message Authentication Code).
            byte[] mac = new byte[0];

            if (mac.Length > 0) _streamReader.Read(mac, 0, mac.Length);

            // Decompress and decode payload data.
            byte[] payload;

            if (_encryptionAlgorithm == null)
                payload = rawPayload;
            else
                payload = _encryptionAlgorithm.Decrypt(rawPayload);

            // Verify MAC of received packet.
            //

            // Read received message.
            using (var msgStream = new MemoryStream(payload))
            {
                using (var msgReader = new SshStreamReader(msgStream))
                {
                    try
                    {
                        byte messageId = msgReader.ReadByte();

                        // Write to debug.
                        Debug.WriteLine(string.Format(">>> {0}", messageId.ToString()));

                        // Check message ID.
                        switch (messageId)
                        {
                            // Standard messages
                            case (byte)SshMessage.Disconnect:
                                ReadMsgDisconnect(msgReader);
                                break;
                            case (byte)SshMessage.Ignore:
                                ReadMsgIgnore(msgReader);
                                break;
                            case (byte)SshMessage.Unimplemented:
                                ReadMsgUnimplemented(msgReader);
                                break;
                            case (byte)SshMessage.Debug:
                                ReadMsgDebug(msgReader);
                                break;
                            case (byte)SshMessage.ServiceRequest:
                                ReadMsgServiceRequest(msgReader);
                                break;
                            case (byte)SshMessage.ServiceAccept:
                                ReadMsgServiceAccept(msgReader);
                                break;
                            case (byte)SshMessage.KexInit:
                                // Store payload of message.
                                _clientKexInitPayload = payload;

                                ReadMsgKexInit(msgReader);
                                break;
                            case (byte)SshMessage.NewKeys:
                                ReadMsgNewKeys(msgReader);
                                break;
                            // Diffie-Hellman kex messages
                            case (byte)SshMessageKexDiffieHellman.Init:
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
                    }
                    catch (Exception ex)
                    {
                        // Fatal error has occurred.
                        Disconnect(SshDisconnectReason.ProtocolError, string.Format("Fatal error: {0}",
                            ex.Message));
                    }
                }
            }

            // Increment sequence number of next packet to be received.
            unchecked { _receivePacketSeqNumber++; }
        }

        protected byte[] ComputeExchangeHash(byte[] hostKey, byte[] clientExchangeValue,
            byte[] serverExchangeValue, byte[] sharedSecret)
        {
            using (var hashInputStream = new MemoryStream())
            {
                using (var hashInputWriter = new SshStreamWriter(hashInputStream))
                {
                    // Write input data to hash stream.
                    hashInputWriter.Write(_clientIdString);
                    hashInputWriter.Write(_serverIdString);
                    hashInputWriter.Write(_clientKexInitPayload);
                    hashInputWriter.Write(_serverKexInitPayload);
                    hashInputWriter.WriteByteString(hostKey);
                    hashInputWriter.Write(clientExchangeValue);
                    hashInputWriter.Write(serverExchangeValue);
                    hashInputWriter.Write(sharedSecret);
                }

                // Return hash of input data.
                return _kexAlgorithm.ComputeHash(hashInputStream.GetBuffer());
            }
        }

        protected byte[] ComputeMac(byte[] unencryptedPacket)
        {
            // Create input data from packet sequency number and unencrypted packet data.
            byte[] inputData = new byte[unencryptedPacket.Length + 4];

            inputData[0] = (byte)((_sendPacketSeqNumber & 0xFF000000) >> 24);
            inputData[1] = (byte)((_sendPacketSeqNumber & 0x00FF0000) >> 16);
            inputData[2] = (byte)((_sendPacketSeqNumber & 0x0000FF00) >> 8);
            inputData[3] = (byte)(_sendPacketSeqNumber & 0x000000FF);
            Buffer.BlockCopy(unencryptedPacket, 0, inputData, 4, unencryptedPacket.Length);

            // Return MAC data.
            return _macAlgorithm.ComputeHash(inputData);
        }

        protected virtual void OnConnected()
        {
            // Create network objects.
            _networkStream = _tcpClient.GetStream();
            _streamWriter = new SshStreamWriter(_networkStream);
            _streamReader = new SshStreamReader(_networkStream);

            _sendPacketSeqNumber = 0;
            _receivePacketSeqNumber = 0;
            _serverIdString = null;
            _clientIdString = null;
            _sessionId = null;

            _kexAlgorithm = null;
            _hostKeyAlgorithm = null;
            _encryptionAlgorithm = null;
            _compressionAlgorithm = null;
            _macAlgorithm = null;

            // Create thread on which to receive data from connection.
            _receiveThread = new Thread(new ThreadStart(ReceiveData));
            _receiveThread.Start();

            // Raise event: client has connected.
            if (Connected != null) Connected(this, new EventArgs());
        }

        protected virtual void OnDisconnected(bool disconnectedRemotely)
        {
            // Raise event: client has disconnected.
            if (Disconnected != null) Disconnected(this, new DisconnectedEventArgs(disconnectedRemotely));
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
                var clientIdVersions = clientIdParts[0].Split('-');

                this.ClientProtocolVersion = clientIdVersions[1];
                this.ClientSoftwareVersion = clientIdVersions[2];
                this.ClientComments = clientIdParts.Length > 1 ? clientIdParts[1] : null;

                // Verify that client protocol version is compatible.
                if (this.ClientProtocolVersion != "2.0")
                {
                    Disconnect(SshDisconnectReason.ProtocolVersionNotSupported,
                        "This server only supports SSH v2.0.");
                    return;
                }

                // Send kex initialization message.
                SendMsgKexInit();

                // Read packets from network stream until connection is closed.
                while (true)
                {
                    // Read next packet.
                    ReadPacket();
                }
            }
            catch (EndOfStreamException)
            {
                // Client disconnected.
                Disconnect(true);
            }
            catch (IOException)
            {
                // Error with network connection.
                Disconnect(false);
            }
            catch (ThreadAbortException)
            {
            }
            finally
            {
            }
        }
    }

    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectedEventArgs(bool disconnectedRemotely)
        {
            this.DisconnectedRemotely = disconnectedRemotely;
        }

        public bool DisconnectedRemotely
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
        NewKeys = 21
    }

    public enum SshMessageKexDiffieHellman : byte
    {
        Init = 30,
        Reply = 31
    }
}
