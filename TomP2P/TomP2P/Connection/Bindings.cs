using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection
{
    /// <summary>
    /// Gathers information about interface bindings.
    /// Here, a user can set the preferences to which addresses to bind the socket.
    /// This class contains two types of information:
    /// 1. The interface/address to listen for incoming connections
    /// 2. How other peers see us
    /// The default is to listen to all interfaces and our outside address is set
    /// to the first interface it finds. If more than one search hint is used, the
    /// combination operation will be "and".
    /// </summary>
    public class Bindings
    {
        public EndPoint WildcardSocket()
        {
            // TODO implement as in Java
            return new IPEndPoint(IPAddress.Any, 0);
        }
    }
}
