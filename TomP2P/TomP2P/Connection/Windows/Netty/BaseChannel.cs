using System.Net;
using System.Net.Sockets;

namespace TomP2P.Connection.Windows.Netty
{
    public abstract class BaseChannel : IChannel
    {
        public event ClosedEventHandler Closed;

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
        /// A Close() method that notfies the subscribed events.
        /// </summary>
        public void Close()
        {
            if (!IsClosed)
            {
                _pipeline.Inactive(); // inactive means getting closed
                IsClosed = true;
                DoClose();
                OnClosed();
            }
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
