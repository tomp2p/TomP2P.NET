using System.Threading;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows.Netty
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
        /// Stops the server channel in an asynchronous manner.
        /// </summary>
        /// <returns></returns>
        Task StopAsync();

        /// <summary>
        /// Serves the incoming requests in an asynchronous manner.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ServiceLoopAsync(CancellationToken ct);
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
