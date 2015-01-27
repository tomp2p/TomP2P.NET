using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Slightly extended <see cref="TcpClient"/>.
    /// </summary>
    public class MyTcpClient : TcpClient, ITcpChannel
    {
        public event ClosedEventHandler Closed;

        public MyTcpClient(IPEndPoint localEndPoint)
            : base(localEndPoint) // bind
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

        public bool IsActive
        {
            get { return this.Active; }
        }

        public Socket Socket
        {
            get { return this.Client; }
        }

    }
}
