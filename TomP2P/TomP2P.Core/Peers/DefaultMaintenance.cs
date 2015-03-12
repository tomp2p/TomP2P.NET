using System;
using System.Collections.Generic;
using NLog;
using TomP2P.Core.Utils;
using TomP2P.Extensions;

namespace TomP2P.Core.Peers
{
    /// <summary>
    /// The default maintenance implementation.
    /// </summary>
    public class DefaultMaintenance : IMaintenance
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly int _peerUrgency;
        private readonly int[] _intervalSeconds;

        private readonly IList<IDictionary<Number160, PeerStatistic>> _peerMapVerified;
        private readonly IList<IDictionary<Number160, PeerStatistic>> _peerMapNonVerified;

        private readonly ConcurrentCacheMap<Number160, PeerAddress> _offlineMap;
        private readonly ConcurrentCacheMap<Number160, PeerAddress> _shutdownMap;
        private readonly ConcurrentCacheMap<Number160, PeerAddress> _exceptionMap;

        /// <summary>
        /// Creates a new maintenance class with the verified and non-verified map.
        /// </summary>
        /// <param name="peerMapVerified">The verified map.</param>
        /// <param name="peerMapNonVerified">The non-verified map.</param>
        /// <param name="offlineMap">The offline map.</param>
        /// <param name="shutdownMap">The shutdown map.</param>
        /// <param name="exceptionMap">The exception map.</param>
        /// <param name="peerUrgency">The number of peers that should be in the verified map. If the
        /// number is lower, urgency is set to yes and we are looking for peers in the non-verified map.</param>
        /// <param name="intervalSeconds"></param>
        private DefaultMaintenance(IList<IDictionary<Number160, PeerStatistic>> peerMapVerified,
            IList<IDictionary<Number160, PeerStatistic>> peerMapNonVerified,
            ConcurrentCacheMap<Number160, PeerAddress> offlineMap,
            ConcurrentCacheMap<Number160, PeerAddress> shutdownMap,
            ConcurrentCacheMap<Number160, PeerAddress> exceptionMap, int peerUrgency, int[] intervalSeconds)
        {
            _peerMapVerified = peerMapVerified;
            _peerMapNonVerified = peerMapNonVerified;
            _offlineMap = offlineMap;
            _shutdownMap = shutdownMap;
            _exceptionMap = exceptionMap;
            _peerUrgency = peerUrgency;
            _intervalSeconds = intervalSeconds;
        }

        /// <summary>
        /// Constructor that initializes the maps as null references. To use this class init must be
        /// called that creates a new class with the private constructor.
        /// </summary>
        /// <param name="peerUrgency"></param>
        /// <param name="intervalSeconds"></param>
        public DefaultMaintenance(int peerUrgency, int[] intervalSeconds)
        {
            _peerMapVerified = null;
            _peerMapNonVerified = null;
            _offlineMap = null;
            _shutdownMap = null;
            _exceptionMap = null;
            _peerUrgency = peerUrgency;
            _intervalSeconds = intervalSeconds;
        }

        public IMaintenance Init(IList<IDictionary<Number160, PeerStatistic>> peerMapVerified, IList<IDictionary<Number160, PeerStatistic>> peerMapNonVerified, ConcurrentCacheMap<Number160, PeerAddress> offlineMap,
            ConcurrentCacheMap<Number160, PeerAddress> shutdownMap, ConcurrentCacheMap<Number160, PeerAddress> exceptionMap)
        {
            return new DefaultMaintenance(peerMapVerified, peerMapNonVerified, offlineMap, shutdownMap, exceptionMap, _peerUrgency, _intervalSeconds);
        }

        /// <summary>
        /// Finds the next peer that should have a maintenance check. Returns null if no maintenance is needed at the moment.
        /// It will return the most important peers first. Importance is as follows: The most important peers are the close
        /// ones in the verified peer map. If a certain threshold in a bag is not reached, the unverified becomes important, too.
        /// </summary>
        /// <param name="notInterestedAddress"></param>
        /// <returns>The next most important peer to check if it is still alive.</returns>
        public PeerStatistic NextForMaintenance(ICollection<PeerAddress> notInterestedAddress)
        {
            if (_peerMapVerified == null || _peerMapNonVerified == null || _offlineMap == null || _shutdownMap == null ||
                _exceptionMap == null)
            {
                throw new ArgumentException("Did not initialize some of the maintenance maps.");
            }
            int peersBefore = 0;
            for (int i = 0; i < Number160.Bits; i++)
            {
                var mapVerified = _peerMapVerified[i];
                bool urgent;
                lock (mapVerified)
                {
                    int size = mapVerified.Count;
                    peersBefore += size;
                    urgent = IsUrgent(i, size, peersBefore);
                }
                if (urgent)
                {
                    var mapNonVerified = _peerMapNonVerified[i];
                    var readyForMaintenance = Next(mapNonVerified);
                    if (readyForMaintenance != null && !notInterestedAddress.Contains(readyForMaintenance.PeerAddress))
                    {
                        Logger.Debug("Check peer {0} from the non-verified map.", readyForMaintenance.PeerAddress);
                        return readyForMaintenance;
                    }
                }
                var readyForMaintenance2 = Next(mapVerified);
                if (readyForMaintenance2 != null && !notInterestedAddress.Contains(readyForMaintenance2.PeerAddress))
                {
                    return readyForMaintenance2;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a peer with its statistics from a bag that needs maintenance.
        /// </summary>
        /// <param name="map">The with all the peers.</param>
        /// <returns>A peer that needs maintenance.</returns>
        private PeerStatistic Next(IDictionary<Number160, PeerStatistic> map)
        {
            lock (map)
            {
                foreach (var peerStatistic in map.Values)
                {
                    if (NeedMaintenance(peerStatistic, _intervalSeconds))
                    {
                        return peerStatistic;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Indicates if it is urgent to search for a peer. This means that we have not enough
        /// peers in the verified map and we need to get one from the non-verified map.
        /// </summary>
        /// <param name="bagIndex">The number of the bag index. The smaller the index, the more important the peer.</param>
        /// <param name="bagSize">The size of the current bag.</param>
        /// <param name="peersBefore">The number of peers we have that are smaller than in this bag index.</param>
        /// <returns>True, if we need urgently a peer from the non-verified map.</returns>
        protected bool IsUrgent(int bagIndex, int bagSize, int peersBefore)
        {
            return bagSize < _peerUrgency;
        }

        /// <summary>
        /// Indicates if a peer needs a maintenance check.
        /// </summary>
        /// <param name="peerStatistic">The peer with its statistics.</param>
        /// <param name="intervalSeconds"></param>
        /// <returns>True, if the peer needs a maintenance check.</returns>
        public static bool NeedMaintenance(PeerStatistic peerStatistic, int[] intervalSeconds)
        {
            int onlineSec = peerStatistic.OnlineTime/1000;
            int index;
            if (onlineSec <= 0)
            {
                index = 0;
            }
            else
            {
                index = intervalSeconds.Length - 1;
                for (int i = 0; i < intervalSeconds.Length; i++)
                {
                    if (intervalSeconds[i] >= onlineSec)
                    {
                        index = i;
                        break;
                    }
                }
            }
            int time = intervalSeconds[index];
            long lastTimeWhenChecked = Convenient.CurrentTimeMillis() - peerStatistic.LastSeenOnline;
            return lastTimeWhenChecked > TimeSpan.FromSeconds(time).TotalSeconds;
        }
    }
}
