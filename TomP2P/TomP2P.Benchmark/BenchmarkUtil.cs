using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NLog;
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

        public static Peer CreateSlave(Peer master, InteropRandom rnd, bool maintenance, bool timeout)
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
        public static ChannelServerConfiguration CreateInfiniteTimeoutChannelServerConfiguration(int port)
        {
            return PeerBuilder.CreateDefaultChannelServerConfiguration()
                .SetIdleTcpSeconds(0)
                .SetIdleUdpSeconds(0)
                .SetConnectionTimeoutTcpMillis(0)
                .SetPorts(new Ports(port, port));
        }

        public static Stopwatch StartBenchmark(string argument)
        {
            // TODO ensure wamup of measured code already took place
            WarmupTimer();
            ReclaimResources();
            Console.WriteLine("{0}: Starting Benchmarking...", argument);
            return Stopwatch.StartNew();
        }

        public static double StopBenchmark(Stopwatch watch, string argument)
        {
            watch.Stop();
            Console.WriteLine("{0}: Stopped Benchmarking.", argument);
            Console.WriteLine("{0}: {1:0.000} ns | {2:0.000} ms | {3:0.000} s", argument, watch.ToNanos(), watch.ToMillis(), watch.ToSeconds());
            // TODO include 2nd GC/OF
            return watch.ToMillis();
        }

        private static long WarmupTimer()
        {
            // force JIT of stopwatch and "use" result
            // otherwise compiler might comment un-used code
            long ticks = 0;
            for (int i = 0; i < 100; i++)
            {
                var watch = Stopwatch.StartNew();
                ticks += watch.ElapsedTicks;
                watch.Stop();
            }
            return ticks;
        }

        private static void ReclaimResources()
        {
            Console.WriteLine("Garbage Collection forced...");
            GC.Collect();
            Console.WriteLine("Object Finalization forced...");
            GC.WaitForPendingFinalizers();
        }

        private static double ToSeconds(this Stopwatch watch)
        {
            return watch.ElapsedTicks / (double)Stopwatch.Frequency;
        }

        private static double ToMillis(this Stopwatch watch)
        {
            return watch.ToSeconds() * 1000;
        }

        private static double ToNanos(this Stopwatch watch)
        {
            return watch.ToSeconds() * 1000000000;
        }

        public static Number160 CreateRandomId(InteropRandom rnd)
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
    }
}
