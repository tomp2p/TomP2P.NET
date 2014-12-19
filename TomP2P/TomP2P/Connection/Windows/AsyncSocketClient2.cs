using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class AsyncSocketClient2
    {
        private readonly Socket _client;
        private readonly IPEndPoint _hostEndpoint;

        public AsyncSocketClient2(string hostName, int hostPort)
        {
            IPHostEntry hostInfo = Dns.GetHostEntry(hostName);

            // instantiate the client endpoint and socket
            // TODO try all addresses of the host
            _hostEndpoint = new IPEndPoint(hostInfo.AddressList[hostInfo.AddressList.Length - 1], hostPort); // TODO client should iterate through addresses
            _client = new Socket(_hostEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); // TODO make UDP
        }

        public async Task ConnectAsync()
        {
            await _client.ConnectAsync(_hostEndpoint);
        }

        public async Task DisconnectAsync()
        {
            await _client.DisconnectAsync(false);
        }

        public async Task<int> SendAsync(byte[] buffer)
        {
            return await _client.SendAsync(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public async Task<int> ReceiveAsync(byte[] buffer)
        {
            return await _client.ReceiveAsync(buffer, 0, buffer.Length, SocketFlags.None);
        }
    }
}
