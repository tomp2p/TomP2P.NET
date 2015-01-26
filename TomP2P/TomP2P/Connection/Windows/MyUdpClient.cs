using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Slightly extended <see cref="UdpClient"/>.
    /// </summary>
    public class MyUdpClient : UdpClient
    {
        public delegate void SocketClosedEventHandler(MyUdpClient sender);

        public event SocketClosedEventHandler Closed;

        public MyUdpClient(IPEndPoint localEndPoint)
            : base(localEndPoint)
        { }

        /// <summary>
        /// A Close() method that notfies the subscribed events.
        /// </summary>
        public void NotifiedClose()
        {
            Close();
            OnClosed();
        }

        protected void OnClosed()
        {
            if (Closed != null)
            {
                Closed(this);
            }
        }
    }
}
