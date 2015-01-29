using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class TcpServerSocket : AsyncServerSocket
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Socket _serverSocket;

        public TcpServerSocket(IPEndPoint localEndPoint, int maxNrOfClients, int bufferSize)
            : base(localEndPoint, maxNrOfClients, bufferSize)
        {
            _serverSocket = new Socket(LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public override void Start()
        {
            Start(_serverSocket);
        }

        public override void Stop()
        {
            Stop(_serverSocket);
        }

        protected override async Task ServiceLoopAsync(ClientToken token)
        {
            while (!IsStopped)
            {
                // reset token for reuse
                token.Reset();

                // accept next client TCP connection
                token.ClientHandler = await _serverSocket.AcceptAsync();
                await ProcessAccept(token);
            }
        }

        private async Task ProcessAccept(ClientToken token)
        {
            Socket handler = token.ClientHandler;

            // as soon as client is connected, post a receive to the connection
            if (handler.Connected)
            {
                try
                {
                    var t = ReceiveAsync(token);
                    await ProcessReceive(t.Result, token);
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Error when processing content received from {0}:\r\n{1}", handler.RemoteEndPoint, ex));
                }
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
                        // TODO process content, maybe use abstract method
                        Array.Copy(token.RecvBuffer, token.SendBuffer, BufferSize);

                        await SendAsync(token);

                        // read next block of content sent by the client
                        var t = ReceiveAsync(token);
                        await ProcessReceive(t.Result, token);
                    }
                    else
                    {
                        // read next block of content sent by the client
                        var t = ReceiveAsync(token);
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

        protected override async Task<int> SendAsync(ClientToken token)
        {
            return await token.ClientHandler.SendAsync(token.SendBuffer, 0, BufferSize, SocketFlags.None);
            // TODO shutdown/close needed?
        }

        protected override async Task<int> ReceiveAsync(ClientToken token)
        {
            return await token.ClientHandler.ReceiveAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None);
        }

        private static void CloseHandlerSocket(Socket handler)
        {
            try
            {
                handler.Shutdown(SocketShutdown.Send);
            }
            catch (Exception)
            {
                // throws if already shutdown
            }
            finally
            {
                handler.Close();
            }
        }
    }
}
