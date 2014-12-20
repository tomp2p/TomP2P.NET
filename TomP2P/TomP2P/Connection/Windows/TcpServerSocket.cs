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

        protected override Socket CreateServerSocket()
        {
            return new Socket(LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        protected override async Task<int> Send(ClientToken token)
        {
            return await token.ClientHandler.SendAsync(token.SendBuffer, 0, BufferSize, SocketFlags.None);
            // TODO shutdown/close needed?
        }

        protected override async Task<int> Receive(ClientToken token)
        {
            return await token.ClientHandler.ReceiveAsync(token.RecvBuffer, 0, BufferSize, SocketFlags.None);
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
