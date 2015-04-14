using System;

namespace TomP2P.Core.Connection.Windows.Netty
{
    // TODO implement this exactly like in Netty
    // - "fire" means the current handler has been processed an the next can be processed
    // - avoiding fire just returns from the handler and doesn't proceed in the pipeline

    /// <summary>
    /// Equivalent to Java Netty's ChannelHandlerContext.
    /// In .NET, this context is implemented with only the functionality required for this project.
    /// One context object is associated per pipeline session.
    /// </summary>
    public class ChannelHandlerContext : DefaultAttributeMap
    {
        private readonly IChannel _channel;
        private readonly PipelineSession _session;

        /// <summary>
        /// Creates a context object for a specific channel and its pipeline.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="session"></param>
        public ChannelHandlerContext(IChannel channel, PipelineSession session)
        {
            _channel = channel;
            _session = session;
        }

        /// <summary>
        /// Results in having the next outbound handler write the provided message.
        /// </summary>
        /// <param name="msg"></param>
        public void FireWrite(object msg)
        {
            // forward to pipeline
            _session.Write(msg);
        }

        /// <summary>
        /// Results in having the next inbound handler read the provided message.
        /// </summary>
        /// <param name="msg"></param>
        public void FireRead(object msg)
        {
            // forward to pipeline
            _session.Read(msg);
        }

        /// <summary>
        /// A channel encountered an exception.
        /// </summary>
        /// <param name="ex"></param>
        public void FireExceptionCaught(Exception ex)
        {
            _session.TriggerException(ex);
        }

        /// <summary>
        /// A channel received a user-defined event.
        /// </summary>
        /// <param name="evt"></param>
        public void FireUserEventTriggered(object evt)
        {
            _session.TriggerUserEvent(evt);
        }

        public void FireTimeout()
        {
            _session.TriggerTimeout();
        }

        public void SkipRestRead()
        {
            _session.SkipRestRead();
        }

        public void Close()
        {
            _channel.Close();
        }

        public IChannel Channel
        {
            get { return _channel; }
        }

        public bool IsTimedOut
        {
            get { return _session.IsTimedOut; }
        }
    }
}
