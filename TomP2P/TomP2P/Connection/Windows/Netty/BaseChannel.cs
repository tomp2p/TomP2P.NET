using System;
using System.Net.Sockets;

namespace TomP2P.Connection.Windows.Netty
{
    public abstract class BaseChannel : IChannel
    {
        public event ClosedEventHandler Closed;

        private readonly Pipeline _pipeline;

        protected BaseChannel(Pipeline pipeline)
        {
            _pipeline = pipeline;
        }

        protected void ProcessOutboundPipeline(Message.Message message)
        {
            if (_pipeline == null)
            {
                throw new NullReferenceException("No pipeline is set for this channel.");
            }
            // query current outbound handlers and execute
        }

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

        public Pipeline Pipeline
        {
            get { return _pipeline; }
        }

        public abstract bool IsUdp { get; }

        public abstract bool IsTcp { get; }
    }
}
