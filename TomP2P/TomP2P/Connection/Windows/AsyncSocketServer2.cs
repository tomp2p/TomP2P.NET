using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class AsyncSocketServer2
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private Socket _serverSocket;
        public int MaxNrOfClients { get; private set; }
        public int BufferSize { get; private set; }

        private static readonly Mutex Mutex = new Mutex(); // to synchronize server execution

        public AsyncSocketServer2(int maxNrOfClients, int bufferSize)
        {
            MaxNrOfClients = maxNrOfClients;
            BufferSize = bufferSize;
        }

        public void Start(IPEndPoint localEndPoint)
        {
            // TODO this is TCP only atm, support UDP too
            _serverSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // bind
            if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // set dual-mode (IPv4 & IPv6) for the socket listener
                // see http://blogs.msdn.com/wndp/archive/2006/10/24/creating-ip-agnostic-applications-part-2-dual-mode-sockets.aspx
                _serverSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                _serverSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port));
            }
            else
            {
                _serverSocket.Bind(localEndPoint);
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
                    int recvBytes = await handler.ReceiveAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None);
                    await ProcessReceive(recvBytes, token);
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

        private async Task Receive(ClientToken token)
        {
            var t = token.ClientHandler.ReceiveAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None);
            await ProcessReceive(t.Result, token);
        }

        private async Task ProcessReceive(int bytesRecv, ClientToken token)
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

                        await handler.SendAsync(token.SendBuffer, 0, BufferSize, SocketFlags.None);

                        // read next block of data sent by the client
                        await Receive(token);
                    }
                    else
                    {
                        // read next block of data sent by the client
                        await Receive(token);
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

        private void CloseHandlerSocket(Socket handler)
        {
            // TODO make UDP
            try
            {
                handler.Shutdown(SocketShutdown.Send);
            }
            catch
            {

            }
            finally
            {
                handler.Close();
            }
        }

        public void Stop()
        {
            _serverSocket.Close();
            Mutex.ReleaseMutex();
        }
    }
}
