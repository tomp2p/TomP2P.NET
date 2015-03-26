using System;
using System.Diagnostics;
using TomP2P.Core.Connection;
using TomP2P.Core.P2P;
using TomP2P.Core.Peers;
using TomP2P.Extensions;

namespace TomP2P.Benchmark
{
    public static class BenchmarkUtil
    {
        /// <summary>
        /// Creates peers for benchmarking. The first peer will be used as the master.
        /// This means that shutting down the master will shut down all other peers as well.
        /// </summary>
        /// <param name="nrOfPeers">Number of peers to create.</param>
        /// <param name="rnd">The random object used for peer ID creation.</param>
        /// <param name="port">The UDP and TCP port.</param>
        /// <param name="maintenance">Indicates whether maintenance should be enabled.</param>
        /// <param name="timeout">Indicates whether timeout should be enabled.</param>
        /// <returns></returns>
        public static Peer[] CreateNodes(int nrOfPeers, InteropRandom rnd, int port, bool maintenance, bool timeout)
        {
            var peers = new Peer[nrOfPeers];

            var masterId = CreateRandomId(rnd);
            var masterMap = new PeerMap(new PeerMapConfiguration(masterId));
            var pb = new PeerBuilder(masterId)
                .SetPorts(port)
                .SetEnableMaintenance(maintenance)
                .SetExternalBindings(new Bindings())
                .SetPeerMap(masterMap);
            if (!timeout)
            {
                pb.SetChannelServerConfiguration(CreateInfiniteTimeoutChannelServerConfiguration(port));
            }
            peers[0] = pb.Start();
            //Logger.Info("Created master peer: {0}.", peers[0].PeerId);

            for (int i = 1; i < nrOfPeers; i++)
            {
                peers[i] = CreateSlave(peers[0], rnd, maintenance, timeout);
            }
            return peers;
        }

        private static Peer CreateSlave(Peer master, InteropRandom rnd, bool maintenance, bool timeout)
        {
            var slaveId = CreateRandomId(rnd);
            var slaveMap = new PeerMap(new PeerMapConfiguration(slaveId).SetPeerNoVerification());
            var pb = new PeerBuilder(slaveId)
                .SetMasterPeer(master)
                .SetEnableMaintenance(maintenance)
                .SetExternalBindings(new Bindings())
                .SetPeerMap(slaveMap);
            if (!timeout)
            {
                pb.SetChannelServerConfiguration(CreateInfiniteTimeoutChannelServerConfiguration(Ports.DefaultPort));
            }
             var slave = pb.Start();
            //Logger.Info("Created slave peer {0}.", slave.PeerId);
            return slave;
        }

        /// <summary>
        /// Creates and returns a ChannelServerConfiguration that has infinite values for all timeouts.
        /// </summary>
        /// <returns></returns>
        private static ChannelServerConfiguration CreateInfiniteTimeoutChannelServerConfiguration(int port)
        {
            return PeerBuilder.CreateDefaultChannelServerConfiguration()
                .SetIdleTcpSeconds(0)
                .SetIdleUdpSeconds(0)
                .SetConnectionTimeoutTcpMillis(0)
                .SetPorts(new Ports(port, port));
        }

        /*public static Stopwatch StartBenchmark()
        {
            WarmupTimer();
            ReclaimResources();
            Console.WriteLine("Starting Benchmarking...");
            return Stopwatch.StartNew();
        }

        public static double StopBenchmark(Stopwatch watch)
        {
            watch.Stop();
            Console.WriteLine("Stopped Benchmarking.");
            Console.WriteLine("{0:0.000} ns | {1:0.000} ms | {2:0.000} s", watch.ToNanos(), watch.ToMillis(), watch.ToSeconds());
            return watch.ToMillis();
        }*/

        public static void WarmupTimer()
        {
            Console.WriteLine("Timer warmup...");
            long anker = 0;
            for (int i = 0; i < 100; i++)
            {
                var watch = Stopwatch.StartNew();
                anker |= watch.ElapsedTicks;
                watch.Restart();
                watch.Stop();
            }
            AnkerTrash(anker);
        }

        public static void ReclaimResources()
        {
            Console.WriteLine("Garbage Collection forced...");
            GC.Collect();
            Console.WriteLine("Object Finalization forced...");
            GC.WaitForPendingFinalizers();
        }

        private static Number160 CreateRandomId(InteropRandom rnd)
        {
            var vals = new int[Number160.IntArraySize];
            for (int i = 0; i < vals.Length; i++)
            {
                vals[i] = rnd.NextInt(Int32.MaxValue);
            }
            return new Number160(vals);
        }

        /// <summary>
        /// This helper method receives an "anker object" just to "throw it away".
        /// This allows such an object to be "used".
        /// </summary>
        /// <param name="anker"></param>
        public static object AnkerTrash(object anker)
        {
            return anker;
        }

        public static void PrintStopwatchProperties()
        {
            Console.WriteLine("Stopwatch.Frequency: {0} ticks/sec", Stopwatch.Frequency);
            Console.WriteLine("Accurate within {0} nanoseconds.", 1000000000L / Stopwatch.Frequency);
            Console.WriteLine("Stopwatch.IsHighResolution: {0}", Stopwatch.IsHighResolution);
        }
    }
}
