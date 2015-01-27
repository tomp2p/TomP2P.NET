using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Netty
{
    public abstract class BaseChannel : IChannel
    {
        public event ClosedEventHandler Closed;

        /// <summary>
        /// A Close() method that notfies the subscribed events.
        /// </summary>
        public void Close()
        {
            DoClose();
            OnClosed();
        }

        protected abstract void DoClose();

        protected void OnClosed()
        {
            if (Closed != null)
            {
                Closed(this);
            }
        }

        public abstract Socket Socket { get; }
    }
}
