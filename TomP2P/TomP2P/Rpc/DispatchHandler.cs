using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;

namespace TomP2P.Rpc
{
    /// <summary>
    /// The dispatcher handlers that can be added to the Dispatcher.
    /// </summary>
    public abstract class DispatchHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The peer bean.
        /// </summary>
        public PeerBean PeerBean { get; private set; }

        /// <summary>
        /// The connection bean.
        /// </summary>
        public ConnectionBean ConnectionBean { get; private set; }

        private bool _sign = false;

        /// <summary>
        /// Creates a handler with a peer bean and a connection bean.
        /// </summary>
        /// <param name="peerBean">The peer bean.</param>
        /// <param name="connectionBean">The connection bean.</param>
        protected DispatchHandler(PeerBean peerBean, ConnectionBean connectionBean)
        {
            PeerBean = peerBean;
            ConnectionBean = connectionBean;
        }
    }
}
