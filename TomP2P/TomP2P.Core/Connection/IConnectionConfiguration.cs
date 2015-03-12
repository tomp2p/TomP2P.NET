
namespace TomP2P.Core.Connection
{
    public interface IConnectionConfiguration
    {
        /// <summary>
        /// The time that a TCP connection can be idle before it is considered not 
        /// active for short-lived connections.
        /// </summary>
        /// <returns></returns>
        int IdleTcpSeconds { get; }

        /// <summary>
        /// The time that a UDP connection can be idle before it is considered not 
        /// active for short-lived connections.
        /// </summary>
        /// <returns></returns>
        int IdleUdpSeconds { get; }

        /// <summary>
        /// The time a TCP connection is allowed to be established.
        /// </summary>
        /// <returns></returns>
        int ConnectionTimeoutTcpMillis { get; }

        /// <summary>
        /// True, if the communication should be TCP. Default is UDP for routing.
        /// </summary>
        /// <returns></returns>
        bool IsForceTcp { get; }

        /// <summary>
        /// True, if the communication should be UDP. Default is TCP for request.
        /// </summary>
        /// <returns></returns>
        bool IsForceUdp { get; }
    }
}
