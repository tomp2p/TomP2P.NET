using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class AsyncSocketClient2
    {
        private readonly Socket _client;
        private readonly IPEndPoint _hostEndpoint;
        private readonly int _bufferSize;

        public AsyncSocketClient2(string hostName, int hostPort, int bufferSize)
        {
            _bufferSize = bufferSize;

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

        public async Task<byte[]> SendAsync()
        {
            
        }

        public async Task<byte[]> ReceiveAsync()
        {
            
        }
    }
}
