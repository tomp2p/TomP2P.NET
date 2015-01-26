using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TomP2P.P2P;
using TomP2P.Peers;

namespace TomP2P.Connection
{
    public class PeerConnection
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public const int HeartBeatMillis = 2000;

        private readonly Semaphore _oneConnection;
        private readonly PeerAddress _remotePeer;
        private readonly ChannelCreator _cc;
        private readonly bool _initiator;

        private readonly IDictionary<TaskCompletionSource<ChannelCreator>, TaskCompletionSource<Message.Message>> _map;
        private readonly TaskCompletionSource<object> _tcsClose;
        private readonly int _heartBeatMillis;

        // TODO find ChannelFuture equivalent and insert throughout whole class

        private PeerConnection(Semaphore oneConnection, PeerAddress remotePeer, ChannelCreator cc, bool initiator,
            IDictionary<TaskCompletionSource<ChannelCreator>, TaskCompletionSource<Message.Message>> map,
            TaskCompletionSource<object> tcsClose, int heartBeatMillis)
        {
            _oneConnection = oneConnection;
            _remotePeer = remotePeer;
            _cc = cc;
            _initiator = initiator;
            _map = map;
            _tcsClose = tcsClose;
            _heartBeatMillis = heartBeatMillis;
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
            _heartBeatMillis = heartBeatMillis;
            _initiator = true;
            _oneConnection = new Semaphore(1, 1);
            _map = new Dictionary<TaskCompletionSource<ChannelCreator>, TaskCompletionSource<Message.Message>>();
            _tcsClose = new TaskCompletionSource<object>();
        }

        /// <summary>
        /// If we already have an open TCP connection, we don't need a channel creator.
        /// </summary>
        /// <param name="remotePeer">The remote peer to connect to.</param>
        /// <param name="heartBeatMillis"></param>
        public PeerConnection(PeerAddress remotePeer, int heartBeatMillis)
        {
            _remotePeer = remotePeer;
            // TODO add channel future
            AddCloseListener();
            _cc = null;
            _heartBeatMillis = heartBeatMillis;
            _initiator = false;
            _oneConnection = new Semaphore(1, 1);
            _map = new Dictionary<TaskCompletionSource<ChannelCreator>, TaskCompletionSource<Message.Message>>();
            _tcsClose = new TaskCompletionSource<object>();
        }

        private void AddCloseListener()
        {
            
        }
    }
}
