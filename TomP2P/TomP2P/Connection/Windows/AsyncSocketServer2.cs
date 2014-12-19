using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class AsyncSocketServer2
    {
        private Socket _serverSocket;
        public int BufferSize { get; private set; }

        public const int MaxNrOfClients = 4;

        private static readonly Mutex Mutex = new Mutex(); // to synchronize server execution

        public AsyncSocketServer2(int bufferSize)
        {
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
            token.Reset();
            await _serverSocket.AcceptAsync().ContinueWith(t => ProcessAccept(t.Result, token));
        }

        private async Task ProcessAccept(Socket handler, ClientToken token)
        {
            // as soon as client is connected, post a receive to the connection
            if (handler.Connected)
            {
                try
                {
                    int recvBytes = await handler.ReceiveAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None);
                    await ProcessReceive(recvBytes, handler, token);

                    // accept next client connection request
                    await AcceptClientConnection(token);
                }
                catch (Exception ex)
                {
                    throw ex; // TODO message
                }
            }
            else
            {
                throw new SocketException((int)SocketError.NotConnected);
            }
        }

        private async Task ProcessReceive(int bytesRecv, Socket handler, ClientToken token)
        {
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

                        await handler.SendAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None);

                        // read next block of data sent by the client
                        await handler.ReceiveAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None).ContinueWith(t => ProcessReceive(t.Result, handler, token));
                    }
                    else
                    {
                        // TODO remove redundancy
                        // read next block of data sent by the client
                        await handler.ReceiveAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None).ContinueWith(t => ProcessReceive(t.Result, handler, token));
                    }
                }
                catch (Exception)
                {
                    // TODO close connection
                    throw;
                }
            }
            else
            {
                // no bytes were sent, client is done sending
                CloseHandlerSocket(handler);
            }
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
