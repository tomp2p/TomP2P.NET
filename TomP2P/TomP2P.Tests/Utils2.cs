using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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
