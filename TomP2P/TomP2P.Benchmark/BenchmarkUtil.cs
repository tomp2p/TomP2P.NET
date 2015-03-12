using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NLog;
using TomP2P.Core.Connection;
using TomP2P.Core.P2P;
using TomP2P.Core.Peers;

namespace TomP2P.Benchmark
{
    public static class BenchmarkUtil
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
            var peers = new Peer[nrOfPeers];

            var masterId = new Number160(rnd);
            var masterMap = new PeerMap(new PeerMapConfiguration(masterId));
            peers[0] = new PeerBuilder(masterId)
                .SetPorts(port)
                .SetEnableMaintenanceRpc(maintenance)
                .SetExternalBindings(new Bindings())
                .SetPeerMap(masterMap)
                .Start();
            Logger.Info("Created master peer: {0}.", peers[0].PeerId);

            for (int i = 1; i < nrOfPeers; i++)
            {
                peers[i] = CreateSlave(peers[0], rnd, maintenance);
            }
            return peers;
        }

        public static Peer CreateSlave(Peer master, Random rnd, bool maintenance)
        {
            var slaveId = new Number160(rnd);
            var slaveMap = new PeerMap(new PeerMapConfiguration(slaveId).SetPeerNoVerification());
            var slave = new PeerBuilder(slaveId)
                .SetMasterPeer(master)
                .SetEnableMaintenanceRpc(maintenance)
                .SetExternalBindings(new Bindings())
                .SetPeerMap(slaveMap)
                .Start();
            Logger.Info("Created slave peer {0}.", slave.PeerId);
            return slave;
        }

        public static void DoBenchmarking(Action func, [CallerMemberName] string caller = "")
        {
            Console.WriteLine("Starting Benchmarking...");
            var watch = Stopwatch.StartNew();

            func();

            watch.Stop();
            Console.WriteLine("Stopped Benchmarking.");
            Console.WriteLine("{0}: {1} ns", caller, watch.ElapsedTicks.ToNanos());
        }

        private static long ToNanos(this long ticks)
        {
            var seconds = ticks / Stopwatch.Frequency;
            return seconds * 1000000000;
        }
    }
}
