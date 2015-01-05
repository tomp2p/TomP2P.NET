using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection
{
    /// <summary>
    /// The configuration for the server.
    /// </summary>
    public class ChannelServerConfiguration : IConnectionConfiguration
    {
        public int IdleTcpSeconds()
        {
            throw new NotImplementedException();
        }

        public int IdleUdpSeconds()
        {
            throw new NotImplementedException();
        }

        public int ConnectionTimeoutTcpMillis()
        {
            throw new NotImplementedException();
        }

        public bool IsForceTcp()
        {
            throw new NotImplementedException();
        }

        public bool IsForceUdp()
        {
            throw new NotImplementedException();
        }
    }
}
