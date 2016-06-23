using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PWOProtocol
{
    public class GameConnection
    {
        public bool IsConnected { get; private set; }

        public event Action<string> PacketReceived;
        public event Action Closed;

        protected const string ServerAddress = "46.28.203.73";
        protected const int ServerPort = 800;
        
        private const string PacketDelimiter = "|.\\\r\n";

        protected TcpClient _client;
        private NetworkStream _stream;
        private Encoding _encoding;
        private byte[] _packetDelimiterBytes;
        private string _pendingBuffer;
        private object _pendingBufferLock;
        private byte[] _readBuffer;

        private int _securityByte;

        public GameConnection()
        {
            _client = new TcpClient();
            _encoding = Encoding.Default;
            _packetDelimiterBytes = _encoding.GetBytes(PacketDelimiter);
            _readBuffer = new byte[4096];
            _pendingBuffer = string.Empty;
            _pendingBufferLock = new object();
        }

        public async Task OpenAsync()
        {
            await OpenConnection();

            IsConnected = true;
            _stream = _client.GetStream();
            ReceiveAsync();
        }

        protected virtual async Task OpenConnection()
        {
            await _client.ConnectAsync(ServerAddress, ServerPort);
        }

        public void Close()
        {
            _client.Close();
        }

        public void Update()
        {
            ReceivePendingPackets();
            if (!IsConnected && Closed != null)
            {
                Closed();
            }
        }

        public void ResetSecurityByte()
        {
            _securityByte = 0;
        }

        public async Task SendAsync(string content)
        {
            byte[] data = _encoding.GetBytes(content);
            data = AppendSecurityByte(data);
            data = ApplyMiddleSwap(data);
            Array.Reverse(data);
            Rc4Encryption.Encrypt(data);
            data = AppendPacketDelimiter(data);

            try
            {
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception)
            {
                IsConnected = false;
            }
        }

        private byte[] AppendSecurityByte(byte[] originalData)
        {
            byte[] securityData = new byte[originalData.Length + PacketDelimiter.Length + 1];
            Array.Copy(originalData, securityData, originalData.Length);
            Array.Copy(_packetDelimiterBytes, 0, securityData, originalData.Length, _packetDelimiterBytes.Length);
            securityData[securityData.Length - 1] = (byte)_securityByte;
            _securityByte = (_securityByte + 1) % 256;
            return securityData;
        }

        private byte[] ApplyMiddleSwap(byte[] originalData)
        {
            byte[] data = new byte[originalData.Length];

            int halfLen = data.Length / 2;
            Array.Copy(originalData, halfLen, data, 0, data.Length - halfLen);
            Array.Copy(originalData, 0, data, data.Length - halfLen, halfLen);

            return data;
        }

        private byte[] AppendPacketDelimiter(byte[] data)
        {
            byte[] buffer = new byte[data.Length + _packetDelimiterBytes.Length];
            Array.Copy(data, buffer, data.Length);
            Array.Copy(_packetDelimiterBytes, 0, buffer, data.Length, _packetDelimiterBytes.Length);
            return buffer;
        }

        private async void ReceiveAsync()
        {
            int numBytes = await MyReadAsync(_readBuffer, 0, _readBuffer.Length);
            if (numBytes <= 0)
            {
                IsConnected = false;
                return;
            }

            string content = _encoding.GetString(_readBuffer, 0, numBytes);
            lock (_pendingBufferLock)
            {
                _pendingBuffer += content;
            }

            ReceiveAsync();
        }

        private Task<int> MyReadAsync(byte[] buffer, int offset, int count)
        {
            return Task.Run(() =>
            {
                try
                {
                    while (IsConnected)
                    {
                        bool result = _client.Client.Poll(1000000, SelectMode.SelectRead);
                        if (result)
                        {
                            int byteCount = _client.Client.Receive(buffer, offset, count, SocketFlags.None);
                            return byteCount;
                        }
                    }
                }
                catch (Exception)
                {
                }
                return 0;
            });
        }

        private void ReceivePendingPackets()
        {
            List<string> packets;
            lock (_pendingBufferLock)
            {
                packets = ExtractAllPendingPackets();
            }
            foreach (string packet in packets)
            {
                PacketReceived?.Invoke(packet);
            }
        }

        private List<string> ExtractAllPendingPackets()
        {
            List<string> packets = new List<string>();
            string packet;
            while ((packet = ExtractOnePendingPacket()) != null)
            {
                packets.Add(packet);
            }
            return packets;
        }

        private string ExtractOnePendingPacket()
        {
            int pos = _pendingBuffer.IndexOf(PacketDelimiter);
            if (pos >= 0)
            {
                string packet = _pendingBuffer.Substring(0, pos);
                _pendingBuffer = _pendingBuffer.Substring(pos + PacketDelimiter.Length);

                byte[] data = _encoding.GetBytes(packet);
                Rc4Encryption.Encrypt(data);
                Array.Reverse(data);
                data = ApplyMiddleSwap(data);

                string content = _encoding.GetString(data);

                content = content.Trim();
                string trimedDelimiter = PacketDelimiter.Trim();
                while (content.EndsWith(trimedDelimiter))
                {
                    content = content.Substring(0, content.Length - trimedDelimiter.Length).Trim();
                }

                return content;
            }
            return null;
        }
    }
}
