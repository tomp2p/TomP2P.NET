using System.Net;

namespace TomP2P.Connection.Windows
{
    public abstract class AsyncClientSocket
    {
        protected readonly IPEndPoint LocalEndPoint;

        protected AsyncClientSocket(IPEndPoint localEndPoint)
        {
            LocalEndPoint = localEndPoint;
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
