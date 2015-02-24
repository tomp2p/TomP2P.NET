using System.Net;
using System.Net.Sockets;

namespace TomP2P.Connection.Windows.Netty
{
    public abstract class BaseChannel : IChannel
    {
        public event ChannelEventHandler Closed;
        public event ChannelEventHandler WriteCompleted;

        protected bool IsClosed;
        private Pipeline _pipeline;

        protected BaseChannel(IPEndPoint localEndPoint)
        {
            LocalEndPoint = localEndPoint;
        }

        public void SetPipeline(Pipeline pipeline)
        {
            _pipeline = pipeline;
            _pipeline.Active(); // active means getting open
        }

        public Pipeline Pipeline
        {
            get { return _pipeline; }
        }

        /// <summary>
        /// A CloseAsync() method that notfies the subscribed events.
        /// </summary>
        public void Close()
        {
            if (!IsClosed)
            {
                _pipeline.Inactive(); // inactive means getting closed
                IsClosed = true;
                DoClose();
                NotifyClosed();
            }
        }

        protected abstract void DoClose();

        private void NotifyClosed()
        {
            if (Closed != null)
            {
                Closed(this);
            }
        }

        /// <summary>
        /// This should be called by all deriving types upon sending finished.
        /// </summary>
        protected void NotifyWriteCompleted()
        {
            if (WriteCompleted != null)
            {
                WriteCompleted(this);
            }
        }

        public abstract Socket Socket { get; }

        public IPEndPoint LocalEndPoint { get; protected set; }

        public IPEndPoint RemoteEndPoint { get; protected set; }

        public abstract bool IsUdp { get; }

        public abstract bool IsTcp { get; }

        public bool IsOpen
        {
            get { return !IsClosed; } // TODO ok?
        }

        public bool IsActive
        {
            get { return Pipeline.IsActive; } // TODO ok?
        }
    }
}
