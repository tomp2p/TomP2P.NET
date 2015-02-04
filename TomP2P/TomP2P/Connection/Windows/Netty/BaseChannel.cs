using System.Net.Sockets;

namespace TomP2P.Connection.Windows.Netty
{
    public abstract class BaseChannel : IChannel
    {
        public event ClosedEventHandler Closed;

        protected bool IsClosed;
        private Pipeline _pipeline;

        public void SetPipeline(Pipeline pipeline)
        {
            _pipeline = pipeline;
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

        public abstract bool IsUdp { get; }

        public abstract bool IsTcp { get; }

        public bool IsOpen
        {
            get { return !IsClosed; } // TODO ok?
        }

        public bool IsActive
        {
            get { return IsOpen && Socket.Connected; } // TODO ok?
        }
    }
}
