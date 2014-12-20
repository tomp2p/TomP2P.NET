using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public abstract class AsyncServerSocket
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public int MaxNrOfClients { get; private set; }
        public int BufferSize { get; private set; }
        
        protected readonly IPEndPoint LocalEndPoint;
        private Socket _serverSocket;
        private static readonly Mutex Mutex = new Mutex(); // to synchronize server execution

        protected AsyncServerSocket(IPEndPoint localEndPoint, int maxNrOfClients, int bufferSize)
        {
            LocalEndPoint = localEndPoint;
            MaxNrOfClients = maxNrOfClients;
            BufferSize = bufferSize;
        }

        protected abstract Socket InstantiateSocket();

        protected abstract Task<int> Send(ClientToken token);

        protected abstract Task<int> Receive(ClientToken token);

        protected abstract void CloseHandlerSocket(Socket handler);

        public void Start()
        {
            _serverSocket = InstantiateSocket();

            // bind
            if (LocalEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // set dual-mode (IPv4 & IPv6) for the socket listener
                // see http://blogs.msdn.com/wndp/archive/2006/10/24/creating-ip-agnostic-applications-part-2-dual-mode-sockets.aspx
                _serverSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                _serverSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, LocalEndPoint.Port));
            }
            else
            {
                _serverSocket.Bind(LocalEndPoint);
            }

            // listen
            if (_serverSocket.ProtocolType == ProtocolType.Tcp)
            {
                _serverSocket.Listen(MaxNrOfClients);
            }

            // accept MaxNrOfClients simultaneous connections
            for (int i = 0; i < MaxNrOfClients; i++)
            {
                AcceptClientConnection(new ClientToken(BufferSize));
            }

            // block current thread to receive incoming messages
            Mutex.WaitOne(); // TODO needed?
        }

        private async Task AcceptClientConnection(ClientToken token)
        {
            // reset token for reuse
            token.Reset();
            token.ClientHandler = await _serverSocket.AcceptAsync();
            await ProcessAccept(token);
        }

        private async Task ProcessAccept(ClientToken token)
        {
            Socket handler = token.ClientHandler;

            // as soon as client is connected, post a receive to the connection
            if (handler.Connected)
            {
                try
                {
                    var t = Receive(token);
                    await ProcessReceive(t.Result, token);
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Error when processing data received from {0}:\r\n{1}", handler.RemoteEndPoint, ex));
                }

                // accept next client connection request, reuse token
                await AcceptClientConnection(token);
            }
            else
            {
                throw new SocketException((int)SocketError.NotConnected);
            }
        }

        protected async Task ProcessReceive(int bytesRecv, ClientToken token)
        {
            Socket handler = token.ClientHandler;

            // check if remote host closed the connection
            if (bytesRecv > 0)
            {
                try
                {
                    if (handler.Available == 0)
                    {
                        // return / send back
                        // TODO process data, maybe use abstract method
                        Array.Copy(token.RecvBuffer, token.SendBuffer, BufferSize);

                        await Send(token);

                        // read next block of data sent by the client
                        var t = Receive(token);
                        await ProcessReceive(t.Result, token);
                    }
                    else
                    {
                        // read next block of data sent by the client
                        var t = Receive(token);
                        await ProcessReceive(t.Result, token);
                    }
                }
                catch (Exception ex)
                {
                    ProcessError(ex, token);
                }
            }
            else
            {
                // no bytes were sent, client is done sending
                CloseHandlerSocket(handler);
            }
        }

        private void ProcessError(Exception ex, ClientToken token)
        {
            var localEp = token.ClientHandler.LocalEndPoint as IPEndPoint;
            Logger.Error("Error with involved endpoint {0}.\r\n{1}", localEp, ex);

            CloseHandlerSocket(token.ClientHandler);
        }

        public void Stop()
        {
            _serverSocket.Close();
            Mutex.ReleaseMutex();
        }
    }
}
