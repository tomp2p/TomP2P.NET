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
        // TODO use buffers

        private Socket _serverSocket;
        private readonly int _bufferSize;
        private readonly byte[] _sendBuffer;
        private readonly byte[] _recvBuffer;

        public const int MaxNrOfClients = 4;
        private int _clientsCount = 0;

        private static Mutex _mutex = new Mutex(); // to synchronize server execution

        public AsyncSocketServer2(int bufferSize)
        {
            _bufferSize = bufferSize;
            _sendBuffer = new byte[bufferSize];
            _recvBuffer = new byte[bufferSize];
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

            // accept
            AcceptLoop();

            // block current thread to receive incoming messages
            _mutex.WaitOne();
        }

        private async Task AcceptLoop()
        {
            // TODO when to set ContinueWith?
            // TODO handle MaxNrOfClients

            // TODO enqueue tasks
            _serverSocket.AcceptAsync().ContinueWith(t => ProcessAccept(t.Result));
        }

        private async Task ProcessAccept(Socket handler)
        {
            // as soon as client is connected, post a receive to the connection
            if (handler.Connected)
            {
                try
                {
                    int recvBytes = await handler.ReceiveAsync(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None);
                    await ProcessReceive(recvBytes, handler);
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

        private async Task ProcessReceive(int bytesRecv, Socket handler)
        {
            // check if remote host closed the connection
            if (bytesRecv > 0)
            {
                try
                {
                    if (handler.Available == 0)
                    {
                        // return / send back
                        // TODO process data
                        Array.Copy(_recvBuffer, _sendBuffer, _recvBuffer.Length);

                        await handler.SendAsync(_sendBuffer, 0, _sendBuffer.Length, SocketFlags.None);
                    }
                    else
                    {
                        // read next block of data sent by the client
                        await handler.ReceiveAsync(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None).ContinueWith(t => ProcessReceive(t.Result, handler));
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
                CloseClientSocket(handler);
            }
        }

        private void CloseClientSocket(Socket handler)
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
            _mutex.ReleaseMutex();
        }
    }
}
