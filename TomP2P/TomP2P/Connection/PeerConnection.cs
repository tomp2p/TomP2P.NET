using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.Windows;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Peers;

namespace TomP2P.Connection
{
    public class PeerConnection : IEquatable<PeerConnection>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public const int HeartBeatMillisConst = 2000;

        private readonly Semaphore _oneConnection;
        public PeerAddress RemotePeer { get; private set; }
        public ChannelCreator ChannelCreator { get; private set; }
        private readonly bool _initiator;

        private readonly IDictionary<TaskCompletionSource<ChannelCreator>, TaskCompletionSource<Message.Message>> _map;
        private readonly TaskCompletionSource<object> _tcsClose;
        public int HeartBeatMillis { get; private set; }

        // these may be called from different threads, but they will never be called concurrently within this library
        private volatile ITcpChannel _channel;

        private PeerConnection(Semaphore oneConnection, PeerAddress remotePeer, ChannelCreator cc, bool initiator,
            IDictionary<TaskCompletionSource<ChannelCreator>, TaskCompletionSource<Message.Message>> map,
            TaskCompletionSource<object> tcsClose, int heartBeatMillis, ITcpChannel channel)
        {
            _oneConnection = oneConnection;
            RemotePeer = remotePeer;
            ChannelCreator = cc;
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
            RemotePeer = remotePeer;
            ChannelCreator = cc;
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
        public PeerConnection(PeerAddress remotePeer, ITcpChannel channel, int heartBeatMillis)
        {
            RemotePeer = remotePeer;
            _channel = channel;
            AddCloseListener(channel);
            ChannelCreator = null;
            HeartBeatMillis = heartBeatMillis;
            _initiator = false;
            _oneConnection = new Semaphore(1, 1);
            _map = new Dictionary<TaskCompletionSource<ChannelCreator>, TaskCompletionSource<Message.Message>>();
            _tcsClose = new TaskCompletionSource<object>();
        }

        public PeerConnection SetChannel(ITcpChannel channel)
        {
            _channel = channel;
            AddCloseListener(channel);
            return this;
        }

        public ITcpChannel Channel
        {
            get { return _channel; }
        }

        public Task CloseTask
        {
            get { return _tcsClose.Task; }
        }

        private void AddCloseListener(ITcpChannel channel)
        {
            channel.Closed += sender =>
            {
                Logger.Debug("About to close the connection {0}, {1}.", channel, _initiator ? "initiator" : "from-dispatcher");
                _tcsClose.SetResult(null); // complete
            };
        }

        public Task Close()
        {
            // cc is not null if we opened the connection
            if (ChannelCreator != null)
            {
                Logger.Debug("Close connection {0}. We were the initiator.", _channel);
                // maybe done on arrival? set close future in any case
                ChannelCreator.ShutdownAsync().ContinueWith(delegate
                {
                    _tcsClose.SetResult(null); // complete
                });
            }
            else
            {
                // cc is null if it is an incoming connection
                // we can close it here or it will be closed when the dispatcher is shut down
                Logger.Debug("Close connection {0}. We are not the initiator.", _channel);
                _channel.Close();
            }
            return _tcsClose.Task;
        }

        public Task<ChannelCreator> AcquireAsync(TaskCompletionSource<Message.Message> tcsResponse)
        {
            var tcsChannelCreator = new TaskCompletionSource<ChannelCreator>();
            return AcquireAsync(tcsChannelCreator, tcsResponse);
        }

        private Task<ChannelCreator> AcquireAsync(TaskCompletionSource<ChannelCreator> tcsChannelCreator, TaskCompletionSource<Message.Message> tcsResponse)
        {
            Logger.Debug("About to acquire a peer connection for {0}.", RemotePeer);
            if (_oneConnection.TryAcquire())
            {
                Logger.Debug("Acquired a peer connection for {0}.", RemotePeer);
                tcsResponse.Task.ContinueWith(delegate
                {
                    _oneConnection.Release();
                    Logger.Debug("Released peer connection for {0}.", RemotePeer);
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
                tcsChannelCreator.SetResult(ChannelCreator);
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

        public bool IsOpen
        {
            get
            {
                if (_channel != null)
                {
                    return _channel.Socket.IsOpen();
                }
                return false;
            }
        }

        public PeerConnection ChangeRemotePeer(PeerAddress remotePeer)
        {
            return new PeerConnection(_oneConnection, remotePeer, ChannelCreator, _initiator, _map, _tcsClose, HeartBeatMillis, _channel);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as PeerConnection);
        }

        public bool Equals(PeerConnection other)
        {
            if (_channel != null)
            {
                return _channel.Equals(other._channel);
            }
            return false;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
