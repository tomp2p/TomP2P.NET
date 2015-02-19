using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Peers;
using TomP2P.Storage;
using TomP2P.Utils;

namespace TomP2P.P2P
{
    /// <summary>
    /// Default implementation for the broadcast. This is a random walk broadcast.
    /// </summary>
    public class DefaultBroadcastHandler : IBroadcastHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly ISet<Number160> DebugCounter = new HashSet<Number160>();

        private const int Nr = 10;
        private const int MaxHopCount = 4;

        private readonly Peer _peer;
        private readonly Random _rnd;
        private readonly ConcurrentCacheMap<Number160, bool> _cache = new ConcurrentCacheMap<Number160, bool>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="peer">The peer that sends the broadcast messages.</param>
        /// <param name="random">Random number, since it is a random walk.</param>
        public DefaultBroadcastHandler(Peer peer, Random random)
        {
            _peer = peer;
            _rnd = random;
        }

        public int BroadcastCounter
        {
            get
            {
                lock (DebugCounter)
                {
                    return DebugCounter.Count;
                }
            }
        }

        public void Receive(Message.Message message)
        {
            var messageKey = message.Key(0);
            IDictionary<Number640, Data> dataMap;
            if (message.DataMap(0) != null)
            {
                dataMap = message.DataMap(0).BackingDataMap;
            }
            else
            {
                dataMap = null;
            }
            int hopCount = message.IntAt(0);
            if (TwiceSeen(messageKey))
            {
                return;
            }
            Logger.Debug("Got broadcast map {0} from {1}.", dataMap, _peer.PeerId);
            lock (DebugCounter)
            {
                DebugCounter.Add(_peer.PeerId);
            }
            if (hopCount < MaxHopCount)
            {
                if (hopCount == 0)
                {
                    FirstPeer(messageKey, dataMap, hopCount, message.IsUdp);
                }
                else
                {
                    OtherPeer(messageKey, dataMap, hopCount, message.IsUdp);
                }
            }
        }

        /// <summary>
        /// If a message is seen for the second time, then we don't want to send this message again.
        /// The cache has a size of 1024 entries and the objects have a default lifetime of 60s.
        /// </summary>
        /// <param name="messageKey">The key of the message.</param>
        /// <returns>True, if this message was sent withing the last 60 seconds.</returns>
        private bool TwiceSeen(Number160 messageKey)
        {
            bool isInCache = _cache.PutIfAbsent(messageKey, true);
            if (isInCache)
            {
                _cache.Put(messageKey, false);
            }
            else
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// The first peer is the initiator. The peer that wants to start the broadcast 
        /// will send it to all its neighbors. Since this peer has an interest in sending, 
        /// it should also work more than the other peers.
        /// </summary>
        /// <param name="messageKey">The key of the message.</param>
        /// <param name="dataMap">The data map to send around.</param>
        /// <param name="hopCounter">The number of hops.</param>
        /// <param name="isUdp">Flag indicating whether the message can be sent with UDP.</param>
        private void FirstPeer(Number160 messageKey, IDictionary<Number640, Data> dataMap, int hopCounter, bool isUdp)
        {
            var list = _peer.PeerBean.PeerMap.All;
            foreach (var peerAddress in list)
            {
                var taskCc = _peer.ConnectionBean.Reservation.CreateAsync(isUdp ? 1 : 0, isUdp ? 0 : 1);
                taskCc.ContinueWith(tcc =>
                {
                    if (!tcc.IsFaulted)
                    {
                        var broadcastBuilder = new BroadcastBuilder(_peer, messageKey);

                        // TODO finish implementation
                        throw new NotImplementedException();
                    }
                    else
                    {
                        Utils.Utils.AddReleaseListener(tcc.Result);
                    }
                });
            }
        }

        /// <summary>
        /// This method is called on relaying peers. We select a random set and we send the message
        /// to those random peers.
        /// </summary>
        /// <param name="messageKey">The key of the message.</param>
        /// <param name="dataMap">The data map to send around.</param>
        /// <param name="hopCounter">The number of hops.</param>
        /// <param name="isUdp">Flag indicating whether the message can be sent with UDP.</param>
        private void OtherPeer(Number160 messageKey, IDictionary<Number640, Data> dataMap, int hopCounter, bool isUdp)
        {
            // TODO finish implementation
            throw new NotImplementedException();
        }
    }
}
