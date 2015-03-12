using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.P2P;
using TomP2P.Peers;

namespace TomP2P.Benchmark
{
    public class Util
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Creates peers for benchmarking. The first peer will be used as the master.
        /// This means that shutting down the master will shut down all other peers as well.
        /// </summary>
        /// <param name="nrOfPeers">Number of peers to create.</param>
        /// <param name="rnd">The random object used for peer ID creation.</param>
        /// <param name="port">The UDP and TCP port.</param>
        /// <param name="maintenance">Indicates whether maintenance should be enabled.</param>
        /// <returns></returns>
        public static Peer[] CreateNodes(int nrOfPeers, Random rnd, int port, bool maintenance)
        {
            var bindings = new Bindings();
            var peers = new Peer[nrOfPeers];

            var masterId = new Number160(rnd);
            var masterMap = new PeerMap(new PeerMapConfiguration(masterId));
            peers[0] = new PeerBuilder(masterId)
                .SetPorts(port)
                .SetEnableMaintenanceRpc(maintenance)
                .SetExternalBindings(bindings)
                .SetPeerMap(masterMap)
                .Start();
            Logger.Info("Created master peer: {0}.", peers[0].PeerId);

            for (int i = 1; i < nrOfPeers; i++)
            {
                var slaveId = new Number160(rnd);
                var slaveMap = new PeerMap(new PeerMapConfiguration(slaveId).SetPeerNoVerification());
                peers[i] = new PeerBuilder(slaveId)
                    .SetMasterPeer(peers[0])
                    .SetEnableMaintenanceRpc(maintenance)
                    .SetExternalBindings(bindings)
                    .SetPeerMap(slaveMap)
                    .Start();
                Logger.Info("Created slave peer {0}: {1}.", i, peers[i].PeerId);
            }
            return peers;
        }
    }
}
