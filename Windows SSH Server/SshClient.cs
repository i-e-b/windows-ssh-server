using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace WindowsSshServer
{
    public class SshClient : IDisposable
    {
        public EventHandler<EventArgs> Connected;
        public EventHandler<DisconnectedEventArgs> Disconnected;

        protected const string _protocolVersion = "2.0"; // Version of SSH protocol.

        protected RNGCryptoServiceProvider _rng;  // Random number generator.
        protected HMAC _macAlgorithm;             // Algorithm to use for computing MAC.

        protected uint _packetSequenceNumber;     // Sequence number of next packet to send.

        protected TcpClient _tcpClient;           // Client TCP connection.
        protected NetworkStream _networkStream;   // Stream for transmitting data across connection.
        protected SshStreamWriter _streamWriter;  // Writes data to network stream.
        protected SshStreamReader _streamReader;  // Reads data from network stream.
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

        public SshClient(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;

            // Create cryptographic objects.
            _rng = new RNGCryptoServiceProvider();
            _macAlgorithm = null;

            // Initialize properties to default.
            this.ServerComments = null;
            this.Languages = null;

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
                    Disconnect();
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

        protected void SendMsgKexInit()
        {
            // Create packet to send.
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
                    msgWriter.Write((from alg in KexAlgorithm.AllAlgorithms select alg.Name).ToArray());

                    // Write list of server host key algorithms.
                    msgWriter.Write((from alg in PublicKeyAlgorithm.AllAlgorithms select alg.Name).ToArray());

                    // Write lists of encryption algorithms.
                    msgWriter.Write((from alg in EncryptionAlgorithm.AllAlgorithms select alg.Name).ToArray());
                    msgWriter.Write((from alg in EncryptionAlgorithm.AllAlgorithms select alg.Name).ToArray());

                    // Write lists of MAC algorithms.
                    msgWriter.Write((from alg in MacAlgorithm.AllAlgorithms select alg.Name).ToArray());
                    msgWriter.Write((from alg in MacAlgorithm.AllAlgorithms select alg.Name).ToArray());

                    // Write lists of compression algorithms.
                    msgWriter.Write((from alg in CompressionAlgorithm.AllAlgorithms select alg.Name).ToArray());
                    msgWriter.Write((from alg in CompressionAlgorithm.AllAlgorithms select alg.Name).ToArray());

                    // Write lists of languages.
                    msgWriter.Write(this.Languages == null ? new string[0] : 
                        (from lang in this.Languages select lang.Tag).ToArray());

                    // Write whether first (guessed) kex packet follows.
                    msgWriter.Write(false);

                    // Write reserved values.
                    msgWriter.Write(0u);
                }

                // Send kex initialization packet.
                SendPacket(msgStream.GetBuffer());
            }
        }

        protected void ReadMsgDisconnect(SshStreamReader messageReader)
        {
            //
        }

        protected void ReadMsgIgnore(SshStreamReader messageReader)
        {
            // Ignore.
        }

        protected void ReadMsgUnimplemented(SshStreamReader messageReader)
        {
            //
        }

        protected void ReadMsgDebug(SshStreamReader messageReader)
        {
            //
        }

        protected void ReadMsgServiceRequest(SshStreamReader messageReader)
        {
            //
        }

        protected void ReadMsgServiceAccept(SshStreamReader messageReader)
        {
            //
        }

        protected void ReadMsgKexInit(SshStreamReader messageReader)
        {
            // Read random cookie.
            byte[] cookie = messageReader.ReadBytes(16);

            // Read list of kex algorithms.
            string[] kexAlgorithms = messageReader.ReadNameList();
            string[] serverHostKeyAlgorithms = messageReader.ReadNameList();
            string[] encryptionAlgorithmsCtoS = messageReader.ReadNameList();
            string[] encryptionAlgorithmsStoC = messageReader.ReadNameList();
            string[] macAlgorithmsCtoS = messageReader.ReadNameList();
            string[] macAlgorithmsStoC = messageReader.ReadNameList();
            string[] compAlgorithmsCtoS = messageReader.ReadNameList();
            string[] compAlgorithmsStoC = messageReader.ReadNameList();
            string[] langsCtoS = messageReader.ReadNameList();
            string[] langsStoC = messageReader.ReadNameList();
            bool firstKexPacketFollows = messageReader.ReadBoolean();
            uint reserved0 = messageReader.ReadUInt32();

            //
        }

        protected void ReadMsgNewKeys(SshStreamReader messageReader)
        {
            //
        }

        protected void SendPacket(byte[] payload)
        {
            // Calculate packet length information.
            byte paddingLength = (byte)(8 - ((5 + payload.Length) % 8));
            if (paddingLength < 4) paddingLength += 8;
            uint packetLength = (uint)payload.Length + paddingLength + 1;

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

            // Write MAC (Message Authentication Code).
            //

            // Increment sequence number of next packet.
            unchecked { _packetSequenceNumber++; }
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
                    }
                }
            }
        }

        protected byte[] ComputeMac(byte[] unencryptedPacket)
        {
            // Create input data from packet sequency number and unencrypted packet data.
            byte[] inputData = new byte[unencryptedPacket.Length + 4];

            inputData[0] = (byte)((_packetSequenceNumber & 0xFF000000) >> 24);
            inputData[1] = (byte)((_packetSequenceNumber & 0x00FF0000) >> 16);
            inputData[2] = (byte)((_packetSequenceNumber & 0x0000FF00) >> 8);
            inputData[3] = (byte)(_packetSequenceNumber & 0x000000FF);
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

            _packetSequenceNumber = 0;

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
                    Disconnect(false);
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
                Disconnect(true);
            }
            catch (IOException ex)
            {
                throw new Exception("CHECK WHAT THIS ERROR MEANS.", ex);
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
