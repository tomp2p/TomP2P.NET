using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class AsyncSocketClient2
    {
        // TODO binding for server-response

        private Socket _tcpClient;
        private Socket _udpClient;
        private IPEndPoint _remoteEp;

        public AsyncSocketClient2()
        {
        }

        /// <summary>
        /// Resolves the server address and connects to it.
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync(string hostName, int hostPort)
        {
            // iterate through all addresses returned from DNS
            IPHostEntry hostInfo = Dns.GetHostEntry(hostName); // TODO make async
            foreach (IPAddress hostAddress in hostInfo.AddressList)
            {
                _remoteEp = new IPEndPoint(hostAddress, hostPort);
                _tcpClient = new Socket(hostAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    await _tcpClient.ConnectAsync(_remoteEp);
                }
                catch (Exception)
                {
                    // connection failed, close and try next address
                    if (_tcpClient != null)
                    {
                        _tcpClient.Close();
                    }
                    continue;
                }
                // connection succeeded
                break;
            }

            // use same remoteEp for UDP
            _udpClient = new Socket(_remoteEp.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        public async Task DisconnectAsync()
        {
            // TCP only
            await _tcpClient.DisconnectAsync(false);
        }

        public async Task<int> SendTcpAsync(byte[] buffer)
        {
            return await _tcpClient.SendAsync(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public async Task<int> ReceiveTcpAsync(byte[] buffer)
        {
            return await _tcpClient.ReceiveAsync(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public async Task<int> SendUdpAsync(byte[] buffer)
        {
            return await _udpClient.SendToAsync(buffer, 0, buffer.Length, SocketFlags.None, _remoteEp);
        }

        public async Task<int> ReceiveUdpAsync(byte[] buffer)
        {
            var ep = _remoteEp as EndPoint;
            return await _udpClient.ReceiveFromAsync(buffer, 0, buffer.Length, SocketFlags.None, ref ep);
        }
    }
}
