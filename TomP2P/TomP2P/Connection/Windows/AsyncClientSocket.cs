using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    // TODO split connect/disconnect for TCP and UDP

    public abstract class AsyncClientSocket
    {
        protected readonly IPEndPoint LocalEndPoint;
        protected IPEndPoint RemoteEndPoint;
        protected Socket ClientSocket;

        protected AsyncClientSocket(IPEndPoint localEndPoint)
        {
            LocalEndPoint = localEndPoint;
        }

        protected abstract Socket CreateClientSocket(AddressFamily addressFamily);

        /// <summary>
        /// Used by TCP and UDP.
        /// - UDP: Remote EndPoint is resolved and stored.
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="hostPort"></param>
        /// <returns></returns>
        public async Task ConnectAsync(string hostName, int hostPort)
        {
            // iterate through all addresses returned from DNS
            IPHostEntry hostInfo = await Dns.GetHostEntryAsync(hostName);
            foreach (IPAddress hostAddress in hostInfo.AddressList)
            {
                RemoteEndPoint = new IPEndPoint(hostAddress, hostPort);
                ClientSocket = CreateClientSocket(hostAddress.AddressFamily);

                try
                {
                    await ClientSocket.ConnectAsync(RemoteEndPoint);
                }
                catch (Exception)
                {
                    // connection failed, close and try next address
                    if (ClientSocket != null)
                    {
                        ClientSocket.Close();
                        ClientSocket = null;
                        RemoteEndPoint = null;
                    }
                    continue;
                }
                // connection succeeded
                break;
            }

            if (ClientSocket == null)
            {
                throw new Exception("Establishing connection failed.");
            }
        }

        /// <summary>
        /// Used by TCP and UDP.
        /// </summary>
        /// <returns></returns>
        public async Task DisconnectAsync()
        {
            await ClientSocket.DisconnectAsync(false);
        }

        /*private void Bind()
        {
            // TODO UDP only?
            // since response from server is expected, bind UDP client to wildcard
            // bind
            try
            {
                if (_localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // set dual-mode (IPv4 & IPv6) for the socket listener
                    // see http://blogs.msdn.com/wndp/archive/2006/10/24/creating-ip-agnostic-applications-part-2-dual-mode-sockets.aspx
                    _udpClient.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                    _udpClient.Bind(new IPEndPoint(IPAddress.IPv6Any, _localEndPoint.Port));
                }
                else
                {
                    _udpClient.Bind(_localEndPoint);
                }
            }
            catch (SocketException ex)
            {
                throw new Exception("Exception during client socket binding.", ex);
            }
        }*/
    }
}
