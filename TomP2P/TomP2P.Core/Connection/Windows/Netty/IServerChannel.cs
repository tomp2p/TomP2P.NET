namespace TomP2P.Core.Connection.Windows.Netty
{
    /// <summary>
    /// Interface for server-side channels that retrieve messages and trigger 
    /// an appropriate service.
    /// </summary>
    public interface IServerChannel : IChannel
    {
        /// <summary>
        /// Starts the server channel.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the server channel.
        /// </summary>
        /// <returns></returns>
        void Stop();
    }

    /// <summary>
    /// Marker interface for server-side TCP channels.
    /// </summary>
    public interface ITcpServerChannel : ITcpChannel, IServerChannel
    {
    }

    /// <summary>
    /// Marker interface for server-side UDP channels.
    /// </summary>
    public interface IUdpServerChannel : IUdpChannel, IServerChannel
    {
    }
}
