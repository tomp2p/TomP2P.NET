
namespace TomP2P.Connection
{
    /// <summary>
    /// The connection configuration with the default settings.
    /// </summary>
    public class DefaultConnectionConfiguration : IConnectionConfiguration
    {
        public int IdleTcpSeconds { get; private set; }
        public int IdleUdpSeconds { get; private set; }
        public int ConnectionTimeoutTcpMillis { get; private set; }
        public bool IsForceTcp { get; private set; }
        public bool IsForceUdp { get; private set; }

        public DefaultConnectionConfiguration()
        {
            IdleTcpSeconds = ConnectionBean.DefaultTcpIdleSeconds;
            IdleUdpSeconds = ConnectionBean.DefaultUdpIdleSeconds;
            ConnectionTimeoutTcpMillis = ConnectionBean.DefaultConnectionTimeoutTcp;
            IsForceTcp = false;
            IsForceUdp = false;
        }

        /// <summary>
        /// Sets the time that a connection can be idle before it is considered 
        /// not active for short-lived connections.
        /// </summary>
        /// <param name="idleTcpSeconds"></param>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetIdleTcpSeconds(int idleTcpSeconds)
        {
            IdleTcpSeconds = idleTcpSeconds;
            return this;
        }

        /// <summary>
        /// Sets the time that a connection can be idle before it is considered 
        /// not active for short-lived connections.
        /// </summary>
        /// <param name="idleUdpSeconds"></param>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetIdleUdpSeconds(int idleUdpSeconds)
        {
            IdleUdpSeconds = idleUdpSeconds;
            return this;
        }

        /// <summary>
        /// Sets the time a TCP  connection is allowed to be established.
        /// </summary>
        /// <param name="connectionTimeoutTcpMillis"></param>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetConnectionTimeoutTcpMillis(int connectionTimeoutTcpMillis)
        {
            ConnectionTimeoutTcpMillis = connectionTimeoutTcpMillis;
            return this;
        }

        /// <summary>
        /// Sets whether the communication should be TCP. Default is UDP for routing.
        /// </summary>
        /// <param name="isForceTcp"></param>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetIsForceTcp(bool isForceTcp)
        {
            IsForceTcp = isForceTcp;
            return this;
        }

        /// <summary>
        /// Sets whether the communication should be TCP. Default is UDP for routing.
        /// </summary>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetIsForceTcp()
        {
            return SetIsForceTcp(true);
        }

        /// <summary>
        /// Sets whether the communication should be UDP. Default is TCP for requests.
        /// </summary>
        /// <param name="isForceUdp"></param>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetIsForceUdp(bool isForceUdp)
        {
            IsForceUdp = isForceUdp;
            return this;
        }

        /// <summary>
        /// Sets whether the communication should be UDP. Default is TCP for requests.
        /// </summary>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetIsForceUdp()
        {
            return SetIsForceUdp(true);
        }
    }
}
