using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection
{
    /// <summary>
    /// The connection configuration with the default settings.
    /// </summary>
    public class DefaultConnectionConfiguration : IConnectionConfiguration
    {
        private bool _forceUdp = false;
        private bool _forceTcp = false;
        private int _idleTcpSeconds = ConnectionBean.DefaultTcpIdleSeconds;
        private int _idleUdpSeconds = ConnectionBean.DefaultUdpIdleSeconds;
        private int _connectionTimeoutTcpMillis = ConnectionBean.DefaultConnectionTimeoutTcp;

        public int IdleTcpSeconds
        {
            get { return _idleTcpSeconds; }
        }

        /// <summary>
        /// Sets the time that a connection can be idle before it is considered 
        /// not active for short-lived connections.
        /// </summary>
        /// <param name="idleTcpSeconds"></param>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetIdleTcpSeconds(int idleTcpSeconds)
        {
            _idleTcpSeconds = idleTcpSeconds;
            return this;
        }

        public int IdleUdpSeconds
        {
            get { return _idleUdpSeconds; }
        }

        /// <summary>
        /// Sets the time that a connection can be idle before it is considered 
        /// not active for short-lived connections.
        /// </summary>
        /// <param name="idleUdpSeconds"></param>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetIdleUdpSeconds(int idleUdpSeconds)
        {
            _idleUdpSeconds = idleUdpSeconds;
            return this;
        }

        public int ConnectionTimeoutTcpMillis
        {
            get { return _connectionTimeoutTcpMillis; }
        }

        /// <summary>
        /// Sets the time a TCP  connection is allowed to be established.
        /// </summary>
        /// <param name="connectionTimeoutTcpMillis"></param>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetConnectionTimeoutTcpMillis(int connectionTimeoutTcpMillis)
        {
            _connectionTimeoutTcpMillis = connectionTimeoutTcpMillis;
            return this;
        }

        public bool IsForceTcp
        {
            get { return _forceTcp; }
        }

        /// <summary>
        /// Sets whether the communication should be TCP. Default is UDP for routing.
        /// </summary>
        /// <param name="isForceTcp"></param>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetIsForceTcp(bool isForceTcp)
        {
            _forceTcp = isForceTcp;
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

        public bool IsForceUdp
        {
            get { return _forceUdp; }
        }

        /// <summary>
        /// Sets whether the communication should be UDP. Default is TCP for requests.
        /// </summary>
        /// <param name="isForceUdp"></param>
        /// <returns>This instance.</returns>
        public DefaultConnectionConfiguration SetIsForceUdp(bool isForceUdp)
        {
            _forceUdp = isForceUdp;
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
