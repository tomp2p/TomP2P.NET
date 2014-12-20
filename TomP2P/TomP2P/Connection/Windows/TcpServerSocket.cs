using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TomP2P.Extensions;

namespace TomP2P.Connection.Windows
{
    public class TcpServerSocket : AsyncServerSocket
    {
        public TcpServerSocket(IPEndPoint localEndPoint, int maxNrOfClients, int bufferSize)
            : base(localEndPoint, maxNrOfClients, bufferSize)
        {
        }

        protected override Socket InstantiateSocket()
        {
            return new Socket(LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        protected override async Task Receive(ClientToken token)
        {
            var t = token.ClientHandler.ReceiveAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None);
            await ProcessReceive(t.Result, token);
        }

        protected override async Task Send(ClientToken token)
        {
            await token.ClientHandler.SendAsync(token.SendBuffer, 0, BufferSize, SocketFlags.None);
            // TODO shutdown/close needed?
        }

        protected override void CloseHandlerSocket(Socket handler)
        {
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
    }
}
