using System;

namespace TomP2P.Connection.Windows.Netty
{
    /// <summary>
    /// Equivalent to Java Netty's ChannelHandlerContext. In .NET, this context is implemented as a class
    /// with only the functionality required for this project.
    /// </summary>
    public class ChannelHandlerContext : DefaultAttributeMap
    {
        private readonly Pipeline _pipeline;
        private readonly IChannel _channel;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pipeline">The pipeline this context is associated with.</param>
        public ChannelHandlerContext(IChannel channel, Pipeline pipeline)
        {
            _channel = channel;
            _pipeline = pipeline;
        }

        /// <summary>
        /// Results in having the next outbound handler write the provided message.
        /// </summary>
        /// <param name="msg"></param>
        public void FireWrite(object msg)
        {
            // forward to pipeline
            _pipeline.Write(msg);
        }

        /// <summary>
        /// Results in having the next inbound handler read the provided message.
        /// </summary>
        /// <param name="msg"></param>
        public void FireRead(object msg)
        {
            // forward to pipeline
            _pipeline.Read(msg);
        }

        public void FireExceptionCaught(Exception ex)
        {
            _pipeline.ExceptionCaught(ex);
        }

        public void Close()
        {
            _channel.Close();
        }

        public IChannel Channel
        {
            get { return _channel; }
        }
    }
}
