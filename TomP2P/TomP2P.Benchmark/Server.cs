using System;
using TomP2P.Core.P2P;
using TomP2P.Extensions;

namespace TomP2P.Benchmark
{
    public static class Server
    {
        public static void Setup()
        {
            Peer server = null;
            try
            {
                var peers = BenchmarkUtil.CreateNodes(1, new InteropRandom(123), 9876, false, false);
                server = peers[0];
                server.RawDataReply(new SendDirectProfiler.SampleRawDataReply());

                var pa = server.PeerAddress;
                Console.WriteLine("Server Peer: {0}.", pa);
                Console.WriteLine("--------------------------------------------------------------------------------------");
                Console.WriteLine("Copy Arguments: {0} {1} {2} {3}.", pa.PeerId, pa.InetAddress, pa.TcpPort, pa.UdpPort);
                Console.WriteLine("--------------------------------------------------------------------------------------");
                Console.WriteLine("Press Enter to shut server down...");
                Console.ReadLine();
            }
            finally
            {
                if (server != null)
                {
                    server.ShutdownAsync().Wait();
                }
            }
        }
    }
}
