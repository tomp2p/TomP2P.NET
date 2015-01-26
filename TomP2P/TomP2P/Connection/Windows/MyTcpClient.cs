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
    /// Slightly extended <see cref="TcpClient"/>.
    /// </summary>
    public class MyTcpClient : TcpClient
    {
        public delegate void SocketClosedEventHandler(MyTcpClient sender);
        public event SocketClosedEventHandler Closed;

        public MyTcpClient()
            : base()
        { }

        /// <summary>
        /// A Close() method that notfies the subscribed events.
        /// </summary>
        public new void Close()
        {
            base.Close();
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
