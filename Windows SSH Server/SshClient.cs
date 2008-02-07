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

namespace WindowsSshServer
{
    internal class SshClient : IDisposable
    {
        public EventHandler<EventArgs> Connected;
        public EventHandler<DisconnectedEventArgs> Disconnected;
        public EventHandler<EventArgs> Error;

        protected const string _protocolVersion = "2.0"; // Implemented version of SSH protocol.

        protected RNGCryptoServiceProvider _rng; // Random number generator.
        protected MacAlgorithm _macAlgorithm;    // Algorithm to use for computing MAC (Message Authentication Code).

        protected uint _sendPacketSeqNumber;     // Sequence number of next packet to send.
        protected uint _receivePacketSeqNumber;  // Sequence number of next packet to be received.
        protected string _sessionId;             // Session identifier, which is the first exchange hash.

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
            _macAlgorithm = null;

            // Initialize properties to default.
            this.ServerComments = null;
            this.Languages = null;

            this.KexAlgorithms = new List<KexAlgorithm>();
            this.PublicKeyAlgorithms = new List<PublicKeyAlgorithm>();
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

        public List<PublicKeyAlgorithm> PublicKeyAlgorithms
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

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    Disconnect(false);
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

        public void Disconnect()
        {
            Disconnect(false);
        }

        public void Disconnect(bool remotely)
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

        protected void Disconnect(SshDisconnectReason reason, string description)
        {
            Disconnect(reason, description, SshLanguage.None);
        }

        protected void Disconnect(SshDisconnectReason reason, string description, SshLanguage language)
        {
            // Send Disconnect message to client.
            SendMsgDisconnect(reason, description, language);

            // Disconnect from client.
            Disconnect(false);
        }

        protected void AddDefaultAlgorithms()
        {
            // Add default algorithms to lists for this client.
            this.KexAlgorithms.Add(new SshDiffieHellmanGroup1Sha1());
            this.KexAlgorithms.Add(new SshDiffieHellmanGroup14Sha1());

            this.PublicKeyAlgorithms.Add(new SshDss());
            this.PublicKeyAlgorithms.Add(new SshRsa());

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

        protected void SendMsgDisconnect(SshDisconnectReason reason, string description, 
            SshLanguage language)
        {
            // Create message to send.
            using (var msgStream = new MemoryStream())
            {
                using (var msgWriter = new SshStreamWriter(msgStream))
                {
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
                    msgWriter.Write(SshMessage.Unimplemented);

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
                    msgWriter.Write(SshMessage.KexInit);

                    // Write random cookie.
                    byte[] cookie = new byte[16];

                    _rng.GetBytes(cookie);
                    msgWriter.Write(cookie);

                    // Write list of kex algorithms.
                    msgWriter.Write((from alg in this.KexAlgorithms select alg.Name).ToArray());

                    // Write list of server host key algorithms.
                    msgWriter.Write((from alg in this.PublicKeyAlgorithms select alg.Name).ToArray());

                    // Write lists of encryption algorithms.
                    msgWriter.Write((from alg in this.EncryptionAlgorithms select alg.Name).ToArray());
                    msgWriter.Write((from alg in this.EncryptionAlgorithms select alg.Name).ToArray());

                    // Write lists of MAC algorithms.
                    msgWriter.Write((from alg in this.MacAlgorithms select alg.Name).ToArray());
                    msgWriter.Write((from alg in this.MacAlgorithms select alg.Name).ToArray());

                    // Write lists of compression algorithms.
                    msgWriter.Write((from alg in this.CompressionAlgorithms select alg.Name).ToArray());
                    msgWriter.Write((from alg in this.CompressionAlgorithms select alg.Name).ToArray());

                    // Write lists of languages.
                    msgWriter.Write(this.Languages == null ? new string[0] :
                        (from lang in this.Languages select lang.Tag).ToArray());

                    // Write whether first (guessed) kex packet follows.
                    msgWriter.Write(false);

                    // Write reserved values.
                    msgWriter.Write(0u);
                }

                // Send Kex Initialization message.
                SendPacket(msgStream.GetBuffer());
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

            //
        }

        protected void ReadMsgNewKeys(SshStreamReader msgReader)
        {
            // Start using new keys and algorithms.
            //
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
            byte[] payload = rawPayload;

            // Verify MAC.
            //

            // Read received message.
            using (var msgStream = new MemoryStream(payload))
            {
                using (var msgReader = new SshStreamReader(msgStream))
                {
                    // Check message ID.
                    switch (msgReader.ReadMessageId())
                    {
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
                            ReadMsgKexInit(msgReader);
                            break;
                        case SshMessage.NewKeys:
                            ReadMsgNewKeys(msgReader);
                            break;
                        default:
                            // Send Unimplemented message back to client.
                            SendMsgUnimplemented();
                            break;
                    }
                }
            }

            // Increment sequence number of next packet to be received.
            unchecked { _receivePacketSeqNumber++; }
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

        protected void OnConnected()
        {
            // Create network objects.
            _networkStream = _tcpClient.GetStream();
            _streamWriter = new SshStreamWriter(_networkStream);
            _streamReader = new SshStreamReader(_networkStream);

            _sendPacketSeqNumber = 0;
            _receivePacketSeqNumber = 0;
            _sessionId = null;

            // Create thread on which to receive data from connection.
            _receiveThread = new Thread(new ThreadStart(ReceiveData));
            _receiveThread.Start();

            // Raise event: client has connected.
            if (Connected != null) Connected(this, new EventArgs());
        }

        protected void OnDisconnected(bool disconnectedRemotely)
        {
            // Raise event: client has disconnected.
            if (Disconnected != null) Disconnected(this, new DisconnectedEventArgs(disconnectedRemotely));
        }

        private void ReceiveData()
        {
            try
            {
                // Send server identification string.
                _streamWriter.WriteLine(string.Format("SSH-{0}-{1}", _protocolVersion,
                    SshClient.SoftwareVersion) + (this.ServerComments == null ? "" :
                    " " + this.ServerComments));

                // Read identification string of client.
                var clientId = _streamReader.ReadLine();
                var clientIdParts = clientId.Split(' ');
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

                // Read packets from network stream.
                while (true)
                {
                    ReadPacket();
                }
            }
            catch (EndOfStreamException)
            {
                // Client disconnected.
                Disconnect(true);
            }
            catch (IOException ex)
            {
                throw new Exception("CHECK WHAT THIS ERROR MEANS EXACTLY.", ex);
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
}
