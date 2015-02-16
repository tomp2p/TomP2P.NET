using System;
using System.Net;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Connection.Windows;
using TomP2P.Extensions.Workaround;
using TomP2P.Futures;
using TomP2P.Peers;

namespace TomP2P.P2P.Builder
{
    public class PingBuilder
    {
        private static readonly Task<PeerAddress> TaskPingShutdown;

        private readonly Peer _peer;

        public PeerAddress PeerAddress { get; private set; }

        public IPAddress InetAddress { get; private set; }

        public int Port { get; private set; }

        public bool IsBroadcast { get; private set; }

        public bool IsTcpPing { get; private set; }

        public PeerConnection PeerConnection { get; private set; }

        private IConnectionConfiguration _connectionConfiguration;

        // static constructor
        static PingBuilder()
        {
            var tcsPingShutdown = new TaskCompletionSource<PeerAddress>();
            tcsPingShutdown.SetException(new TaskFailedException("Peer is shutting down."));
            TaskPingShutdown = tcsPingShutdown.Task;
        }

        public PingBuilder(Peer peer)
        {
            _peer = peer;
        }

        public PingBuilder NotifyAutomaticFutures(Task future)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        public Task<PeerAddress> Start()
        {
            if (_peer.IsShutdown)
            {
                return TaskPingShutdown;
            }

            if (_connectionConfiguration == null)
            {
                _connectionConfiguration = new DefaultConnectionConfiguration();
            }

            if (IsBroadcast)
            {
                return PingBroadcast(Port);
            }
            else
            {
                if (PeerAddress != null)
                {
                    if (IsTcpPing)
                    {
                        return Ping(PeerAddress, false);
                    }
                    else
                    {
                        return Ping(PeerAddress, true);
                    }
                }
                else if (InetAddress != null)
                {
                    if (IsTcpPing)
                    {
                        return Ping(new IPEndPoint(InetAddress, Port), Number160.Zero, false);
                    }
                    else
                    {
                        return Ping(new IPEndPoint(InetAddress, Port), Number160.Zero, true);
                    }
                }
                else if (PeerConnection != null)
                {
                    return PingPeerConnection(PeerConnection);
                }
                else
                {
                    throw new ArgumentException("Cannot ping. Peer address or inet address required.");
                }
            }
        }

        private Task<PeerAddress> PingBroadcast(int port)
        {
            var tcsPing = new TaskCompletionSource<PeerAddress>();
            var bindings = _peer.ConnectionBean.Sender.ChannelClientConfiguration.BindingsOutgoing;
            int size = bindings.BroadcastAddresses.Count;

            var taskLateJoin = new TcsLateJoin<Task<Message.Message>>(size, 1);
            if (size > 0)
            {
                var taskChannelCreator = _peer.ConnectionBean.Reservation.CreateAsync(size, 0);
                Utils.Utils.AddReleaseListener(taskChannelCreator, tcsPing.Task);
                taskChannelCreator.ContinueWith(taskCc =>
                {
                    if (!taskCc.IsFaulted)
                    {
                        AddPingListener(tcsPing, taskLateJoin); // TODO works?
                        for (int i = 0; i < size; i++)
                        {
                            var broadcastAddress = bindings.BroadcastAddresses[i];
                            var peerAddress = new PeerAddress(Number160.Zero, broadcastAddress, port, port);
                            var taskValidBroadcastResponse = _peer.PingRpc.PingBroadcastUdpAsync(peerAddress, taskCc.Result,
                                _connectionConfiguration);
                            if (!taskLateJoin.Add(taskValidBroadcastResponse))
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (taskCc.Exception != null)
                        {
                            tcsPing.SetException(taskCc.Exception);
                        }
                        else
                        {
                            tcsPing.SetException(new TaskFailedException("TODO"));
                        }
                    }
                });
            }
            else
            {
                tcsPing.SetException(new TaskFailedException("No broadcast address found. Cannot ping nothing."));
            }
            return tcsPing.Task;
        }

        /// <summary>
        /// Pings a peer.
        /// </summary>
        /// <param name="address">The address of the remote peer.</param>
        /// <param name="peerId"></param>
        /// <param name="isUdp">True, for UDP. False, for TCP.</param>
        /// <returns></returns>
        private Task<PeerAddress> Ping(IPEndPoint address, Number160 peerId, bool isUdp)
        {
            return Ping(new PeerAddress(peerId, address), isUdp);
        }

        /// <summary>
        /// Pings a peer.
        /// </summary>
        /// <param name="peerAddress">The peer address of the remote peer.</param>
        /// <param name="isUdp">True, for UDP. False, for TCP.</param>
        /// <returns></returns>
        private Task<PeerAddress> Ping(PeerAddress peerAddress, bool isUdp)
        {
            var tcsPing = new TaskCompletionSource<PeerAddress>();
            var requestHandler = _peer.PingRpc.Ping(peerAddress, _connectionConfiguration);
            if (isUdp)
            {
                // ping UDP
                var taskCc = _peer.ConnectionBean.Reservation.CreateAsync(1, 0);
                Utils.Utils.AddReleaseListener(taskCc, tcsPing.Task);
                taskCc.ContinueWith(tcc =>
                {
                    if (!tcc.IsFaulted)
                    {
                        var taskResponse = requestHandler.SendUdpAsync(tcc.Result);
                        AddPingListener(tcsPing, taskResponse);
                    }
                    else
                    {
                        if (tcc.Exception != null)
                        {
                            tcsPing.SetException(tcc.Exception);
                        }
                        else
                        {
                            tcsPing.SetException(new TaskFailedException("Task<ChannelCreator> failed."));
                        }
                    }
                });
            }
            else
            {
                // ping TCP
                var taskCc = _peer.ConnectionBean.Reservation.CreateAsync(0, 1);
                Utils.Utils.AddReleaseListener(taskCc, tcsPing.Task);
                taskCc.ContinueWith(tcc =>
                {
                    if (!tcc.IsFaulted)
                    {
                        var taskResponse = requestHandler.SendTcpAsync(tcc.Result);
                        AddPingListener(tcsPing, taskResponse);
                    }
                    else
                    {
                        if (tcc.Exception != null)
                        {
                            tcsPing.SetException(tcc.Exception);
                        }
                        else
                        {
                            tcsPing.SetException(new TaskFailedException("Task<ChannelCreator> failed."));
                        }
                    }
                });
            }
            return tcsPing.Task;
        }

        private Task<PeerAddress> PingPeerConnection(PeerConnection peerConnection)
        {
            var tcsPing = new TaskCompletionSource<PeerAddress>();
            var requestHandler = _peer.PingRpc.Ping(peerConnection.RemotePeer, _connectionConfiguration);
            var taskCc = peerConnection.AcquireAsync(requestHandler.TcsResponse);
            taskCc.ContinueWith(tcc =>
            {
                if (!tcc.IsFaulted)
                {
                    requestHandler.TcsResponse.Task.Result.SetKeepAlive(true); // TODO correct?
                    var taskResponse = requestHandler.SendTcpAsync(peerConnection);
                    AddPingListener(tcsPing, taskResponse);
                }
                else
                {
                    if (tcc.Exception != null)
                    {
                        tcsPing.SetException(tcc.Exception);
                    }
                    else
                    {
                        tcsPing.SetException(new TaskFailedException("Task<ChannelCreator> failed."));
                    }
                }
            });
            return tcsPing.Task;
        }

        private static void AddPingListener(TaskCompletionSource<PeerAddress> tcsPing, TcsLateJoin<Task<Message.Message>> tcsLateJoin)
        {
            // TODO works?
            // we have one successful reply
            tcsLateJoin.Task.ContinueWith(tlj =>
            {
                if (!tlj.IsFaulted && tcsLateJoin.TasksDone().Count > 0)
                {
                    var taskResponse = tcsLateJoin.TasksDone()[0];
                    tcsPing.SetResult(taskResponse.Result.Sender);
                }
                else
                {
                    if (tlj.Exception != null)
                    {
                        tcsPing.SetException(tlj.Exception);
                    }
                    else
                    {
                        tcsPing.SetException(new TaskFailedException("No successful ping reply received."));
                    }
                }
            });
        }

        private static void AddPingListener(TaskCompletionSource<PeerAddress> tcsPing,
            Task<Message.Message> taskResponse)
        {
            // TODO works?
            taskResponse.ContinueWith(taskResponse2 =>
            {
                if (!taskResponse2.IsFaulted)
                {
                    tcsPing.SetResult(taskResponse2.Result.Sender);
                }
                else
                {
                    if (taskResponse2.Exception != null)
                    {
                        tcsPing.SetException(taskResponse2.Exception);
                    }
                    else
                    {
                        tcsPing.SetException(new TaskFailedException("No successful ping reply received."));
                    }
                }
            });
        }

        public PingBuilder SetPeerAddress(PeerAddress peerAddress)
        {
            PeerAddress = peerAddress;
            return this;
        }

        public PingBuilder SetInetAddress(IPAddress inetAddress)
        {
            InetAddress = inetAddress;
            return this;
        }

        public PingBuilder SetPort(int port)
        {
            Port = port;
            return this;
        }

        public PingBuilder SetInetSocketAddress(IPEndPoint socket)
        {
            InetAddress = socket.Address;
            Port = socket.Port;
            return this;
        }

        public PingBuilder SetIsBroadcast()
        {
            return SetIsBroadcast(true);
        }

        public PingBuilder SetIsBroadcast(bool isBroadcast)
        {
            IsBroadcast = isBroadcast;
            return this;
        }

        public PingBuilder SetIsTcpPing()
        {
            return SetIsTcpPing(true);
        }

        public PingBuilder SetIsTcpPing(bool isTcpPing)
        {
            IsTcpPing = isTcpPing;
            return this;
        }

        public PingBuilder SetPeerConnection(PeerConnection peerConnection)
        {
            PeerConnection = peerConnection;
            return this;
        }
    }
}
