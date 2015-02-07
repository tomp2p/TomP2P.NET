using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Connection.Windows;
using TomP2P.Peers;

namespace TomP2P.P2P.Builder
{
    public class PingBuilder
    {
        // TODO use Task<PeerAddress> instead?
        private static readonly TaskCompletionSource<PeerAddress> FuturePingShutdown;

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
            FuturePingShutdown = new TaskCompletionSource<PeerAddress>();
            FuturePingShutdown.SetException(new TaskFailedException("Peer is shutting down."));
        }

        public PingBuilder(Peer peer)
        {
            _peer = peer;
        }

        public PingBuilder NotifyAutomaticFutures(Task future)
        {
            _peer.NotifyAutomaticFutures(future);
            return this;
        }

        public TaskCompletionSource<PeerAddress> Start()
        {
            if (_peer.IsShutdown)
            {
                return FuturePingShutdown;
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

        private TaskCompletionSource<PeerAddress> PingBroadcast(int port)
        {
            // TODO this method is very tricky to port --> check!
            var tcsPing = new TaskCompletionSource<PeerAddress>();
            var bindings = _peer.ConnectionBean.Sender.ChannelClientConfiguration.BindingsOutgoing;
            int size = bindings.BroadcastAddresses.Count;

            // .NET-specific
            // K = FutureResponse = Task<Message>
            // FutureLateJoin = Task.WhenAll(FutureResponse[]) = Task.WhenAll(Task<Message>[])
            // = Task<Message[]>
            // --> TCS<Task<Message[]>>
            var futureResponses = new Task<Message.Message>[1];
            var tcsLateJoin = new TaskCompletionSource<Task<Message.Message[]>>(futureResponses);

            if (size > 0)
            {
                var taskChannelCreator = _peer.ConnectionBean.Reservation.CreateAsync(size, 0);
                Utils.Utils.AddReleaseListener(taskChannelCreator, tcsPing.Task);
                taskChannelCreator.ContinueWith(taskCc =>
                {
                    if (!taskCc.IsFaulted)
                    {
                        AddPingListener(tcsPing, tcsLateJoin.Task); // TODO works?
                        for (int i = 0; i < size; i++)
                        {
                            var broadcastAddress = bindings.BroadcastAddresses[i];
                            var peerAddress = new PeerAddress(Number160.Zero, broadcastAddress, port, port);
                            var taskValidBroadcastResponse = _peer.PingRpc.PingBroadcastUdpAsync(peerAddress, taskCc.Result,
                                _connectionConfiguration);
                            // .NET-specific:
                            if (tcsLateJoin.Task.IsCompleted)
                            {
                                break;
                            }
                            else
                            {
                                var futureResponses2 = (Task<Message.Message>[]) tcsLateJoin.Task.AsyncState;
                                futureResponses2[0] = taskValidBroadcastResponse; // since there is only 1 item in the array, this works --> if more needed, take list
                                // TODO how to complete TCSLateJoin, when all AsyncState tasks are done???
                                // --> in Java, each added future is attached a completion listener -> handler checks counter and decides if FLJ completes
                                taskValidBroadcastResponse.ContinueWith(validResponse =>
                                {
                                    // succeed TLJ, since it has only one future item
                                    var result = new TaskCompletionSource<Message.Message[]>();


                                    tcsLateJoin.SetResult(new Task<Message.Message[]> );
                                });


                                /*
                                Task<Message.Message>[] futureResponses2 = (Task<Message.Message>[])tcsLateJoin.Task.AsyncState;
                                Task<Message.Message[]> whenAllTask = Task.WhenAll(futureResponses2);
                                tcsLateJoin.SetResult(whenAllTask);
                                */
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
        }

        /// <summary>
        /// Pings a peer.
        /// </summary>
        /// <param name="address">The address of the remote peer.</param>
        /// <param name="peerId"></param>
        /// <param name="isUdp">True, for UDP. False, for TCP.</param>
        /// <returns></returns>
        private TaskCompletionSource<PeerAddress> Ping(IPEndPoint address, Number160 peerId, bool isUdp)
        {
            return Ping(new PeerAddress(peerId, address), isUdp);
        }

        /// <summary>
        /// Pings a peer.
        /// </summary>
        /// <param name="peerAddress">The peer address of the remote peer.</param>
        /// <param name="isUdp">True, for UDP. False, for TCP.</param>
        /// <returns></returns>
        private TaskCompletionSource<PeerAddress> Ping(PeerAddress peerAddress, bool isUdp)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        private TaskCompletionSource<PeerAddress> PingPeerConnection(PeerConnection peerConnection)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        private static void AddPingListener(TaskCompletionSource<PeerAddress> tcsPing, Task<Task<Message.Message[]>> taskLateJoin)
        {
            // TODO works?
            // we have one successful reply
            taskLateJoin.ContinueWith(tlj =>
            {
                // .NET-specific: tlj.Result.Result == future.futuresDone() -> means they're completed
                if (!tlj.IsFaulted && tlj.Result.Result.Length > 0) // TODO works?
                {
                    var responseMessage = tlj.Result.Result[0];
                    tcsPing.SetResult(responseMessage.Sender);
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

        private void AddPingListener(TaskCompletionSource<PeerAddress> tcsPing,
            TaskCompletionSource<Message.Message> tcsResponse)
        {
            // TODO works?
            tcsResponse.Task.ContinueWith(taskResponse =>
            {
                if (!taskResponse.IsFaulted)
                {
                    tcsPing.SetResult(taskResponse.Result.Sender);
                }
                else
                {
                    if (taskResponse.Exception != null)
                    {
                        tcsPing.SetException(taskResponse.Exception);
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
