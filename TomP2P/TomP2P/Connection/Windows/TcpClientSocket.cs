using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class TcpClientSocket : AsyncClientSocket
    {
        private Socket _tcpClient;

        public TcpClientSocket(IPEndPoint localEndPoint) 
            : base(localEndPoint)
        {
        }

        public async Task ConnectAsync(string hostName, int hostPort)
        {
            // iterate through all addresses returned from DNS
            IPHostEntry hostInfo = await Dns.GetHostEntryAsync(hostName);
            foreach (IPAddress hostAddress in hostInfo.AddressList)
            {
                var remoteEp = new IPEndPoint(hostAddress, hostPort);
                _tcpClient = new Socket(hostAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    await _tcpClient.ConnectAsync(remoteEp);
                }
                catch (Exception)
                {
                    // connection failed, close and try next address
                    if (_tcpClient != null)
                    {
                        _tcpClient.Close();
                        _tcpClient = null;
                    }
                    continue;
                }
                // connection succeeded
                break;
            }

            if (_tcpClient == null)
            {
                throw new Exception("Establishing connection failed.");
            }
        }

        public async Task DisconnectAsync()
        {
            await _tcpClient.DisconnectAsync(false);
        }

        public async Task<int> SendAsync(byte[] buffer)
        {
            return await _tcpClient.SendAsync(buffer, 0, buffer.Length, SocketFlags.None);

            // TODO TCP shutdown/close needed?
        }

        public async Task<int> ReceiveAsync(byte[] buffer)
        {
            return await _tcpClient.ReceiveAsync(buffer, 0, buffer.Length, SocketFlags.None);

            // TODO loop as long as recvBytes == 0?
            // TODO shutdown/close needed?
        }
    }
}
