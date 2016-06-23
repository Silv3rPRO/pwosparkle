using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PWOProtocol
{
    public class SocksConnection : GameConnection
    {
        public int SocksVersion { get; private set; }
        public string SocksAddress { get; private set; }
        public int SocksPort { get; private set; }
        public string SocksUsername { get; private set; }
        public string SocksPassword { get; private set; }

        public SocksConnection(int version, string address, int port, string username = "", string password = "")
        {
            SocksVersion = version;
            SocksAddress = address;
            SocksPort = port;
            SocksUsername = username;
            SocksPassword = password;
        }

        protected override async Task OpenConnection()
        {
            await _client.ConnectAsync(SocksAddress, SocksPort);
            if (SocksVersion == 5)
            {
                await HandleSocks5();
            }
            else if (SocksVersion == 4)
            {
                await HandleSocks4();
            }
        }

        private async Task HandleSocks5()
        {
            byte[] buffer = new byte[1024];

            buffer[0] = 0x05;
            buffer[1] = 0x02;
            buffer[2] = 0x00;
            buffer[3] = 0x02;
            await _client.GetStream().WriteAsync(buffer, 0, 4);
            await _client.GetStream().ReadAsync(buffer, 0, 2);

            if (buffer[0] != 5)
            {
                throw new Exception("Received invalid version from the proxy server");
            }

            if (buffer[1] == 0x02)
            {
                byte[] username = Encoding.ASCII.GetBytes(SocksUsername);
                byte[] password = Encoding.ASCII.GetBytes(SocksPassword);

                int i = 0;
                buffer[i++] = 0x01;

                buffer[i++] = (byte)SocksUsername.Length;
                Array.Copy(username, 0, buffer, i, username.Length);
                i += username.Length;

                buffer[i++] = (byte)SocksPassword.Length;
                Array.Copy(password, 0, buffer, i, password.Length);
                i += username.Length;

                await _client.GetStream().WriteAsync(buffer, 0, i);
                await _client.GetStream().ReadAsync(buffer, 0, 2);

                if (buffer[0] != 1)
                {
                    throw new Exception("Received invalid authentication version from the proxy server");
                }

                if (buffer[1] != 0)
                {
                    throw new Exception("The proxy server has refused the username/password authentication");
                }
            }
            else if (buffer[1] != 0x00)
            {
                throw new Exception("Received invalid authentication method from the proxy server");
            }

            byte[] address = IPAddress.Parse(ServerAddress).GetAddressBytes();
            byte[] port = BitConverter.GetBytes((ushort)ServerPort);
            Array.Reverse(port);

            buffer[0] = 0x05;
            buffer[1] = 0x01;
            buffer[2] = 0x00;
            buffer[3] = 0x01;
            Array.Copy(address, 0, buffer, 4, 4);
            Array.Copy(port, 0, buffer, 8, 2);
            await _client.GetStream().WriteAsync(buffer, 0, 10);
            await _client.GetStream().ReadAsync(buffer, 0, 10);

            if (buffer[0] != 5)
            {
                throw new Exception("Received invalid version from the proxy server");
            }
            if (buffer[1] != 0)
            {
                throw new Exception("Received connection failure from the proxy server");
            }
            if (buffer[3] != 0x01)
            {
                throw new Exception("Received invalid address type from the proxy server");
            }
        }

        private async Task HandleSocks4()
        {
            byte[] buffer = new byte[1024];

            byte[] address = IPAddress.Parse(ServerAddress).GetAddressBytes();
            byte[] port = BitConverter.GetBytes((ushort)ServerPort);
            Array.Reverse(port);

            buffer[0] = 0x04;
            buffer[1] = 0x01;
            Array.Copy(port, 0, buffer, 2, 2);
            Array.Copy(address, 0, buffer, 4, 4);
            buffer[8] = 0x00;
            await _client.GetStream().WriteAsync(buffer, 0, 9);
            await _client.GetStream().ReadAsync(buffer, 0, 8);

            if (buffer[0] != 0)
            {
                throw new Exception("Received invalid header from the proxy server");
            }
            if (buffer[1] != 0x5a)
            {
                throw new Exception("The proxy server rejected the connection");
            }
        }
    }
}
