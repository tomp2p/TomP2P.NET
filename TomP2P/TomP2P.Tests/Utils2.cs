using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Constraints;
using TomP2P.Connection;
using TomP2P.Message;
using TomP2P.P2P;
using TomP2P.Peers;

namespace TomP2P.Tests
{
    public class Utils2
    {
        public static readonly long TheAnswer = 42L;
        public static readonly long TheAnsert2 = 43L;

        public static Message.Message CreateDummyMessage()
        {
            return CreateDummyMessage(false, false);
        }

        public static Message.Message CreateDummyMessage(bool firewallUdp, bool firewallTcp)
        {
            return CreateDummyMessage(new Number160("0x4321"), "127.0.0.1", 8001, 8002, new Number160("0x1234"),
                "127.0.0.1", 8003, 8004, (sbyte)0, TomP2P.Message.Message.MessageType.Request1, firewallUdp, firewallTcp);
        }

        public static Message.Message CreateDummyMessage(Number160 idSender, String inetSender, int tcpPortSendor,
            int udpPortSender, Number160 idRecipient, String inetRecipient, int tcpPortRecipient,
            int udpPortRecipient, sbyte command, Message.Message.MessageType type, bool firewallUdp, bool firewallTcp)
        {
            var message = new Message.Message();

            PeerAddress n1 = CreateAddress(idSender, inetSender, tcpPortSendor, udpPortSender, firewallUdp, firewallTcp);
            message.SetSender(n1);
            //
            PeerAddress n2 = CreateAddress(idRecipient, inetRecipient, tcpPortRecipient, udpPortRecipient, firewallUdp, firewallTcp);
            message.SetRecipient(n2);
            message.SetType(type);
            message.SetCommand(command);
            return message;
        }

        public static PeerAddress[] CreateDummyAddresses(int size, int portTcp, int portUdp)
        {
            var pa = new PeerAddress[size];
            for (int i = 0; i < size; i++)
            {
                pa[i] = CreateAddress(i + 1, portTcp, portUdp);
            }
            return pa;
        }

        public static PeerAddress CreateAddress(int iid, int portTcp, int portUdp)
        {
            var id = new Number160(iid);
            var address = IPAddress.Parse("127.0.0.1");
            return new PeerAddress(id, address, portTcp, portUdp);
        }

        public static PeerAddress CreateAddress(Number160 id)
        {
            return CreateAddress(id, "127.0.0.1", 8005, 8006, false, false);
        }

        public static PeerAddress CreateAddress()
        {
            return CreateAddress(new Number160("0x5678"), "127.0.0.1", 8005, 8006, false, false);
        }

        public static PeerAddress CreateAddress(int id)
        {
            return CreateAddress(new Number160(id), "127.0.0.1", 8005, 8006, false, false);
        }

        public static PeerAddress CreateAddress(String id)
        {
            return CreateAddress(new Number160(id), "127.0.0.1", 8005, 8006, false, false);
        }

        public static PeerAddress CreateAddress(Number160 idSender, String inetSender, int tcpPortSender,
            int udpPortSender, bool firewallUdp, bool firewallTcp)
        {
            IPAddress inetSend = IPAddress.Parse(inetSender); // TODO correct port
            var peerSocketAddress = new PeerSocketAddress(inetSend, tcpPortSender, udpPortSender);
            var n1 = new PeerAddress(idSender, peerSocketAddress, firewallTcp, firewallUdp, false, PeerAddress.EmptyPeerSocketAddresses);
            return n1;
        }

        public static Peer[] CreateNodes(int nrOfPeers, Random rnd, int port)
        {
            return CreateNodes(nrOfPeers, rnd, port, null);
        }

        public static Peer[] CreateNodes(int nrOfPeers, Random rnd, int port, IAutomaticTask automaticTask)
        {
            return CreateNodes(nrOfPeers, rnd, port, automaticTask, false);
        }

        /// <summary>
        /// Creates peers for testing. The first peer will be used as the master.
        /// This means that shutting down the master will shut down all other peers as well.
        /// </summary>
        /// <param name="nrOfPeers"></param>
        /// <param name="rnd"></param>
        /// <param name="port"></param>
        /// <param name="automaticTask"></param>
        /// <param name="maintenance"></param>
        /// <returns></returns>
        public static Peer[] CreateNodes(int nrOfPeers, Random rnd, int port, IAutomaticTask automaticTask, bool maintenance)
        {
            var bindings = new Bindings();
            var peers = new Peer[nrOfPeers];
            if (automaticTask != null)
            {
                var peerId = new Number160(rnd);
                var peerMap = new PeerMap(new PeerMapConfiguration(peerId));
                peers[0] = new PeerBuilder(peerId)
                    .SetPorts(port)
                    .SetEnableMaintenanceRpc(maintenance)
                    .SetExternalBindings(bindings)
                    .SetPeerMap(peerMap)
                    .Start()
                    .AddAutomaticTask(automaticTask);
            }
            else
            {
                var peerId = new Number160(rnd);
                var peerMap = new PeerMap(new PeerMapConfiguration(peerId));
                peers[0] = new PeerBuilder(peerId)
                    .SetPorts(port)
                    .SetEnableMaintenanceRpc(maintenance)
                    .SetExternalBindings(bindings)
                    .SetPeerMap(peerMap)
                    .Start();
            }
            Console.WriteLine("Created master peer: {0}.", peers[0].PeerId);
            for (int i = 1; i < nrOfPeers; i++)
            {
                if (automaticTask != null)
                {
                    var peerId = new Number160(rnd);
                    var peerMap = new PeerMap(new PeerMapConfiguration(peerId));
                    peers[i] = new PeerBuilder(peerId)
                        .SetMasterPeer(peers[0])
                        .SetEnableMaintenanceRpc(maintenance)
                        .SetExternalBindings(bindings)
                        .SetPeerMap(peerMap)
                        .Start()
                        .AddAutomaticTask(automaticTask);
                }
                else
                {
                    var peerId = new Number160(rnd);
                    var peerMap = new PeerMap(new PeerMapConfiguration(peerId).SetPeerNoVerification());
                    peers[i] = new PeerBuilder(peerId)
                        .SetMasterPeer(peers[0])
                        .SetEnableMaintenanceRpc(maintenance)
                        .SetExternalBindings(bindings)
                        .SetPeerMap(peerMap)
                        .Start();
                }
                Console.WriteLine("Created slave peer {0}: {1}.", i, peers[i].PeerId);
            }
            return peers;
        }

        /// <summary>
        /// Creates and returns a ChannelServerConfiguration that has infinite values for all timeouts.
        /// </summary>
        /// <returns></returns>
        public static ChannelServerConfiguration CreateInfiniteTimeoutChannelServerConfiguration(int portUdp, int portTcp)
        {
            return PeerBuilder.CreateDefaultChannelServerConfiguration()
                .SetIdleTcpSeconds(0)
                .SetIdleUdpSeconds(0)
                .SetConnectionTimeoutTcpMillis(0)
                .SetPorts(new Ports(portTcp, portUdp));
        }

        public static IConnectionConfiguration CreateInfiniteConfiguration()
        {
            return new DefaultConnectionConfiguration()
                .SetConnectionTimeoutTcpMillis(Int32.MaxValue)
                .SetIdleUdpSeconds(Int32.MaxValue)
                .SetIdleTcpSeconds(Int32.MaxValue);
        }

        /// <summary>
        /// Creates and returns a MaintenanceTask that has an infinite interval.
        /// </summary>
        /// <returns></returns>
        public static MaintenanceTask CreateInfiniteIntervalMaintenanceTask()
        {
            return new MaintenanceTask()
                .SetIntervalMillis(Int32.MaxValue);
        }
    }
}
