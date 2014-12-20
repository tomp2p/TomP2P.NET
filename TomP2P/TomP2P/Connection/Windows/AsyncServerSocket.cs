using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows
{
    public abstract class AsyncServerSocket
    {
        public int MaxNrOfClients { get; private set; }
        public int BufferSize { get; private set; }

        protected readonly IPEndPoint LocalEndPoint;

        protected volatile bool IsStopped;

        protected AsyncServerSocket(IPEndPoint localEndPoint, int maxNrOfClients, int bufferSize)
        {
            LocalEndPoint = localEndPoint;
            MaxNrOfClients = maxNrOfClients;
            BufferSize = bufferSize;
        }

        public abstract void Start();

        public abstract void Stop();

        protected void Start(Socket serverSocket)
        {
            // bind
            if (LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // set dual-mode (IPv4 & IPv6) for the socket listener
                // see http://blogs.msdn.com/wndp/archive/2006/10/24/creating-ip-agnostic-applications-part-2-dual-mode-sockets.aspx
                serverSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                serverSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, LocalEndPoint.Port));
            }
            else
            {
                serverSocket.Bind(LocalEndPoint);
            }

            // listen, TCP-only
            if (serverSocket.ProtocolType == ProtocolType.Tcp)
            {
                serverSocket.Listen(MaxNrOfClients);
            }

            // accept MaxNrOfClients simultaneous connections
            for (int i = 0; i < MaxNrOfClients; i++)
            {
                ServiceLoopAsync(new ClientToken(BufferSize));
            }
            IsStopped = false;
        }

        protected void Stop(Socket serverSocket)
        {
            if (serverSocket.ProtocolType == ProtocolType.Tcp)
            {
                try
                {
                    serverSocket.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException)
                {
                    // TODO exception is thrown, correct to ignore it here?
                }
            }
            serverSocket.Close();
            IsStopped = true;
        }

        protected abstract Task ServiceLoopAsync(ClientToken token);

        protected abstract Task<int> SendAsync(ClientToken token);

        protected abstract Task<int> ReceiveAsync(ClientToken token);
    }
}
