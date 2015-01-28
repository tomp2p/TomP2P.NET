namespace TomP2P.Connection.Windows.Netty
{
    /// <summary>
    /// Equivalent to Java Netty's ChannelHandlerContext. In .NET, this context is implemented as a class
    /// with only the functionality required for this project.
    /// </summary>
    public class ChannelHandlerContext
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

        public void Write(object msg)
        {
            // forward to pipeline
            _pipeline.Write(msg);
        }

        public void FireExceptionCaught(System.Exception ex)
        {
            throw new System.NotImplementedException();
        }

        public IChannel Channel
        {
            get { return _channel; }
        }
    }
}
