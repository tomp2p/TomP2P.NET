using System;

namespace TomP2P.Connection
{
    /// <summary>
    /// Stores port information.
    /// </summary>
    public class Ports
    {
        // the maximal port number, 2^16
        public const int MaxPort = 65535;
        // IANA recommends to use ports higher than 49152
        public const int MinDynPort = 49152;
        // the default port of TomP2P
        public const int DefaultPort = 7700;

        private const int Range = MaxPort - MinDynPort;
        private static readonly Random Random = new Random(); // TODO use InteropRandom?

        // provide this information if you know your mapping beforehand
        // i.e., manual port-forwarding
        /// <summary>
        /// The external TCP port, how other peers see us.
        /// </summary>
        public int TcpPort { get; private set; }
        /// <summary>
        /// The external UDP port, how other peers see us.
        /// </summary>
        public int UdpPort { get; private set; }
        private readonly bool _randomPorts;

        /// <summary>
        /// Creates random ports for TCP and UDP. The random ports start from port 49152.
        /// </summary>
        public Ports()
            : this(-1, -1)
        { }

        /// <summary>
        /// Creates a Ports class that stores port information.
        /// </summary>
        /// <param name="tcpPort">The external TCP port, how other peers will see us. If the provided port is negative, a random port will be used.</param>
        /// <param name="udpPort">The external UDP port, how other peers will see us. If the provided port is negative, a random port will be used.</param>
        public Ports(int tcpPort, int udpPort)
        {
            _randomPorts = tcpPort < 0 && udpPort < 0;
            TcpPort = tcpPort < 0 ? (Random.Next(Range) + MinDynPort) : tcpPort;
            UdpPort = udpPort < 0 ? (Random.Next(Range) + MinDynPort) : udpPort;
        }

        /// <summary>
        /// True, if the user specified both ports in advance. This tells us that the
        /// user knows about the ports and did a manual port-forwarding.
        /// </summary>
        public bool IsManualPort
        {
            get { return !_randomPorts; }
        }
    }
}
