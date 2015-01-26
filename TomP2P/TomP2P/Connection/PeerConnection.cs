using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions;
using TomP2P.P2P;
using TomP2P.Peers;

namespace TomP2P.Connection
{
    public class PeerConnection
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public const int HeartBeatMillisConst = 2000;

        private readonly Semaphore _oneConnection;
        private readonly PeerAddress _remotePeer;
        private readonly ChannelCreator _cc;
        private readonly bool _initiator;

        private readonly IDictionary<TaskCompletionSource<ChannelCreator>, TaskCompletionSource<Message.Message>> _map;
        private readonly TaskCompletionSource<object> _tcsClose;
        public int HeartBeatMillis { get; private set; }

        // these may be called from different threads, but they will never be called concurrently within this library
        private volatile TcpClient _channel;

        private PeerConnection(Semaphore oneConnection, PeerAddress remotePeer, ChannelCreator cc, bool initiator,
            IDictionary<TaskCompletionSource<ChannelCreator>, TaskCompletionSource<Message.Message>> map,
            TaskCompletionSource<object> tcsClose, int heartBeatMillis, TcpClient channel)
        {
            _oneConnection = oneConnection;
            _remotePeer = remotePeer;
            _cc = cc;
            _initiator = initiator;
            _map = map;
            _tcsClose = tcsClose;
            HeartBeatMillis = heartBeatMillis;
            _channel = channel;
        }

        /// <summary>
        /// If we don't have an open TCP connection, we first need a channel creator to open a channel.
        /// </summary>
        /// <param name="remotePeer">The remote peer to connect to.</param>
        /// <param name="cc">The channel creator where we can open a TCP connection.</param>
        /// <param name="heartBeatMillis"></param>
        public PeerConnection(PeerAddress remotePeer, ChannelCreator cc, int heartBeatMillis)
        {
            _remotePeer = remotePeer;
            _cc = cc;
            HeartBeatMillis = heartBeatMillis;
            _initiator = true;
            _oneConnection = new Semaphore(1, 1);
            _map = new Dictionary<TaskCompletionSource<ChannelCreator>, TaskCompletionSource<Message.Message>>();
            _tcsClose = new TaskCompletionSource<object>();
        }

        /// <summary>
        /// If we already have an open TCP connection, we don't need a channel creator.
        /// </summary>
        /// <param name="remotePeer">The remote peer to connect to.</param>
        /// <param name="channel">The already open TCP channel.</param>
        /// <param name="heartBeatMillis"></param>
        public PeerConnection(PeerAddress remotePeer, TcpClient channel, int heartBeatMillis)
        {
            _remotePeer = remotePeer;
            _channel = channel;
            AddCloseListener(channel);
            _cc = null;
            HeartBeatMillis = heartBeatMillis;
            _initiator = false;
            _oneConnection = new Semaphore(1, 1);
            _map = new Dictionary<TaskCompletionSource<ChannelCreator>, TaskCompletionSource<Message.Message>>();
            _tcsClose = new TaskCompletionSource<object>();
        }

        public PeerConnection SetChannel(TcpClient channel)
        {
            _channel = channel;
            AddCloseListener(channel);
            return this;
        }

        public TcpClient Channel
        {
            get { return _channel; }
        }

        public Task CloseTask
        {
            get { return _tcsClose.Task; }
        }

        private void AddCloseListener(TcpClient channel)
        {
            // TODO implement
            // TODO use Close() event from MyUdpClient
            throw new NotImplementedException();
        }

        public Task Close()
        {
            // TODO implement
            throw new NotImplementedException();
        }

        public Task<ChannelCreator> AcquireAsync(TaskCompletionSource<Message.Message> tcsResponse)
        {
            var tcsChannelCreator = new TaskCompletionSource<ChannelCreator>();
            return AcquireAsync(tcsChannelCreator, tcsResponse);
        }

        private Task<ChannelCreator> AcquireAsync(TaskCompletionSource<ChannelCreator> tcsChannelCreator, TaskCompletionSource<Message.Message> tcsResponse)
        {
            Logger.Debug("About to acquire a peer connection for {0}.", _remotePeer);
            if (_oneConnection.TryAcquire())
            {
                Logger.Debug("Acquired a peer connection for {0}.", _remotePeer);
                tcsResponse.Task.ContinueWith(delegate
                {
                    _oneConnection.Release();
                    Logger.Debug("Released peer connection for {0}.", _remotePeer);
                    lock (_map)
                    {
                        // TODO works?
                        foreach (var entry in _map.ToList()) // iterate over list-copy
                        {
                            _map.Remove(entry); // remove from original list
                            AcquireAsync(entry.Key, entry.Value);
                        }
                    }
                });
                tcsChannelCreator.SetResult(_cc);
                return tcsChannelCreator.Task;
            }
            else
            {
                lock (_map)
                {
                    _map.Add(tcsChannelCreator, tcsResponse);
                }
            }
            return tcsChannelCreator.Task;
        }
    }
}
