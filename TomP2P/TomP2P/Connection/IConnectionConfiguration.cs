
namespace TomP2P.Connection
{
    public interface IConnectionConfiguration
    {
        /// <summary>
        /// The time that a TCP connection can be idle before it is considered not 
        /// active for short-lived connections.
        /// </summary>
        /// <returns></returns>
        int IdleTcpSeconds();

        /// <summary>
        /// The time that a UDP connection can be idle before it is considered not 
        /// active for short-lived connections.
        /// </summary>
        /// <returns></returns>
        int IdleUdpSeconds();

        /// <summary>
        /// The time a TCP connection is allowed to be established.
        /// </summary>
        /// <returns></returns>
        int ConnectionTimeoutTcpMillis();

        /// <summary>
        /// Set to true, if the communication should be TCP. Default is UDP for routing.
        /// </summary>
        /// <returns></returns>
        bool IsForceTcp();

        /// <summary>
        /// Set to true, if the communication should be UDP. Default is TCP for request.
        /// </summary>
        /// <returns></returns>
        bool IsForceUdp();
    }
}
