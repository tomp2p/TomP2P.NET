
namespace TomP2P.Connection.Windows
{
    public abstract class AsyncClientSocket
    {
        //protected readonly IPEndPoint LocalEndPoint;

        /*protected AsyncClientSocket(IPEndPoint localEndPoint)
        {
            LocalEndPoint = localEndPoint;
        }*/

        public delegate void SocketClosedEventHandler(AsyncClientSocket sender);

        public event SocketClosedEventHandler Closed;

        public abstract void Close();

        protected void OnClosed()
        {
            if (Closed != null)
            {
                Closed(this);
            }
        }
    }
}
