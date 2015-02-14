using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;
using TomP2P.Futures;
using TomP2P.P2P.Builder;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.P2P
{
    /// <summary>
    /// The routing mechanism.
    /// </summary>
    public class RoutingMechanism
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public VolatileReferenceArray<TaskCompletionSource<Message.Message>> TcsResponses { get; private set; }
        public TcsRouting TcsRoutingResponse { get; private set; }
        private readonly ICollection<IPeerFilter> _peerFilters;

        private SortedSet<PeerAddress> _queueToAsk;
        private SortedSet<PeerAddress> _alreadyAsked;
        /// <summary>
        /// The peers that have certain data stored on it.
        /// </summary>
        public SortedDictionary<PeerAddress, DigestInfo> DirectHits { get; private set; }
        private SortedSet<PeerAddress> _potentialHits;

        private int _nrNoNewInfo = 0;
        private int _nrFailures = 0;
        private int _nrSuccess = 0;

        public int MaxDirectHits { get; set; }
        public int MaxNoNewInfo { get; set; }
        public int MaxFailures { get; set; }
        public int MaxSucess { get; set; }
        /// <summary>
        /// True, if we should stop creating more tasks. False, otherwise.
        /// </summary>
        public bool IsStopCreatingNewFutures { get; set; }

        /// <summary>
        /// Creates the routing mechanism. Make sure to set the Max* fields.
        /// </summary>
        /// <param name="tcsResponses">The current task responses that are running.</param>
        /// <param name="tcsRoutingResponse">The response task from this routing request.</param>
        /// <param name="peerFilters"></param>
        public RoutingMechanism(VolatileReferenceArray<TaskCompletionSource<Message.Message>> tcsResponses,
            TcsRouting tcsRoutingResponse, ICollection<IPeerFilter> peerFilters)
        {
            TcsResponses = tcsResponses;
            TcsRoutingResponse = tcsRoutingResponse;
            _peerFilters = peerFilters;
        }

        /// <summary>
        /// The number of parallel requests. The number is determined by the length of the
        /// task response array.
        /// </summary>
        public int Parallel
        {
            get { return TcsResponses.Length; }
        }

        /// <summary>
        /// Gets the TCS at position i.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public TaskCompletionSource<Message.Message> TcsResponse(int i)
        {
            return TcsResponses.Get(i);
        }

        /// <summary>
        /// Sets a response at position i to the given value and returns the old response.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="tcsResponse"></param>
        /// <returns></returns>
        public TaskCompletionSource<Message.Message> TcsResponse(int i,
            TaskCompletionSource<Message.Message> tcsResponse)
        {
            return TcsResponses.GetAndSet(i, tcsResponse);
        }

        /// <summary>
        /// Sets the queue that contains the peers that will be queried in the future.
        /// </summary>
        /// <param name="queueToAsk"></param>
        /// <returns>This instance.</returns>
        public RoutingMechanism SetQueueToAsk(SortedSet<PeerAddress> queueToAsk)
        {
            _queueToAsk = queueToAsk;
            return this;
        }

        /// <summary>
        /// Gets the queue that contains the peers that will be queried in the future.
        /// </summary>
        public SortedSet<PeerAddress> QueueToAsk
        {
            get
            {
                lock (this)
                {
                    return _queueToAsk;
                }
            }
        }

        /// <summary>
        /// Sets the peers we have already queried. We need to store them to not ask
        /// the same peers again.
        /// </summary>
        /// <param name="alreadyAsked"></param>
        /// <returns>This instance.</returns>
        public RoutingMechanism SetAlreadyAsked(SortedSet<PeerAddress> alreadyAsked)
        {
            _alreadyAsked = alreadyAsked;
            return this;
        }

        /// <summary>
        /// Gets the peers we have already queried. We need to store them to not ask
        /// the same peers again.
        /// </summary>
        public SortedSet<PeerAddress> AlreadyAsked
        {
            get
            {
                lock (this)
                {
                    return _queueToAsk;
                }
            }
        }

        /// <summary>
        /// Sets the potential hits. These are those reported by other peers that 
        /// we did not check if they contain certain data.
        /// </summary>
        /// <param name="potentialHits"></param>
        /// <returns></returns>
        public RoutingMechanism SetPotentialHits(SortedSet<PeerAddress> potentialHits)
        {
            _potentialHits = potentialHits;
            return this;
        }

        /// <summary>
        /// Gets the potential hits. These are those reported by other peers that 
        /// we did not check if they contain certain data.
        /// </summary>
        public SortedSet<PeerAddress> PotentialHits
        {
            get
            {
                lock (this)
                {
                    return _potentialHits;
                }
            }
        }

        /// <summary>
        /// Sets the peers that have certain data stored on it.
        /// </summary>
        /// <param name="directHits"></param>
        /// <returns></returns>
        public RoutingMechanism SetDirectHits(SortedDictionary<PeerAddress, DigestInfo> directHits)
        {
            DirectHits = directHits;
            return this;
        }

        public PeerAddress PollFirstInQueueToAsk()
        {
            lock (this)
            {
                return _queueToAsk.PollFirst();
            }
        }

        public PeerAddress PollRandomInQueueToAsk(Random rnd)
        {
            lock (this)
            {
                return Utils.Utils.PollRandom(_queueToAsk, rnd);
            }
        }

        public void AddToAlreadyAsked(PeerAddress next)
        {
            lock (this)
            {
                _alreadyAsked.Add(next);
            }
        }

        public void SetNeighbors(RoutingBuilder routingBuilder)
        {
            lock (this)
            {
                // TODO doesn't this create a deadlock due to property-locks?
                TcsRoutingResponse.SetNeighbors(DirectHits, PotentialHits, AlreadyAsked, routingBuilder.IsBootstrap, routingBuilder.IsRoutingToOthers);
            }
        }

        /// <summary>
        /// Cancels the task that cancels the underlying tasks as well.
        /// </summary>
        public void Cancel()
        {
            int len = TcsResponses.Length;
            for (int i = 0; i < len; i++)
            {
                var tcsResponse = TcsResponses.Get(i);
                if (tcsResponse != null)
                {
                    tcsResponse.SetCanceled(); // TODO works?
                }
            }
        }

        public void AddPotentialHits(PeerAddress remotePeer)
        {
            lock (this)
            {
                _potentialHits.Add(remotePeer);
            }
        }

        public bool EvaluateFailed()
        {
            return (++_nrFailures) > MaxFailures;
        }

        public bool EvaluateSuccess(PeerAddress remotePeer, DigestInfo digestBean, ICollection<PeerAddress> newNeighbors,
            bool last, Number160 locationKey)
        {
            bool finished;
            lock (this)
            {
                FilterPeers(newNeighbors, _alreadyAsked, _queueToAsk, locationKey);
                if (EvaluateDirectHits(remotePeer, DirectHits, digestBean, MaxDirectHits))
                {
                    // stop immediately
                    Logger.Debug("Enough direct hits found: {0}.", DirectHits);
                    finished = true;
                    IsStopCreatingNewFutures = true;
                }
                else if ((++_nrSuccess) > MaxSucess)
                {
                    // wait until pending tasks are finished
                    Logger.Debug("Max success reached: {0}.", _nrSuccess);
                    finished = last;
                    IsStopCreatingNewFutures = true;
                }
                else if (EvaluateInformation(newNeighbors, _queueToAsk, _alreadyAsked, MaxNoNewInfo))
                {
                    // wait untul pending tasks are finished
                    Logger.Debug("No new information for the {0} time.");
                    finished = last;
                    IsStopCreatingNewFutures = true;
                }
                else
                {
                    // continue
                    finished = false;
                    IsStopCreatingNewFutures = false;
                }
            }
            return finished;
        }

        private void FilterPeers(ICollection<PeerAddress> newNeighbors, IEnumerable<PeerAddress> alreadyAsked,
            IEnumerable<PeerAddress> queueToAsk, Number160 locationKey)
        {
            if (_peerFilters == null || _peerFilters.Count == 0)
            {
                return;
            }
            var all = new List<PeerAddress>();
            all.AddRange(alreadyAsked);
            all.AddRange(queueToAsk);
            foreach (var newNeighbor in newNeighbors.ToList()) // iterate over list-copy
            {
                foreach (var filter in _peerFilters)
                {
                    if (filter.Reject(newNeighbor, all, locationKey))
                    {
                        newNeighbors.Remove(newNeighbor); // remove from original list
                    }
                }
            }
        }

        /// <summary>
        /// For Get() requests we can finish earlier if we found the data we were looking for.
        /// This checks if we reached the end of our search.
        /// </summary>
        /// <param name="remotePeer">The remote peer that gave us this digest information.</param>
        /// <param name="directHits">The result dictionary that will store how many peers reported that data is there.</param>
        /// <param name="digestBean">The digest information coming from the remote peer.</param>
        /// <param name="maxDirectHits">The max. number of direct hits. E.g., finding the value we were looking for
        /// before we can stop.</param>
        /// <returns>True, if we can stop. False, if we should continue.</returns>
        private static bool EvaluateDirectHits(PeerAddress remotePeer, IDictionary<PeerAddress, DigestInfo> directHits,
            DigestInfo digestBean, int maxDirectHits)
        {
            if (digestBean.Size > 0)
            {
                directHits.Add(remotePeer, digestBean);
                if (directHits.Count >= maxDirectHits)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if we reached the end of our search.
        /// </summary>
        /// <param name="newNeighbors">The new neighbors we just received.</param>
        /// <param name="queueToAsk">The peers that are in the queue to be asked.</param>
        /// <param name="alreadyAsked">The peers we have already asked.</param>
        /// <param name="maxNoNewInfo">The maximum number of replies from neighbors that
        /// do not give us closer peers.</param>
        /// <returns>True, if we should stop. False, if we should continue with the routing.</returns>
        private bool EvaluateInformation(ICollection<PeerAddress> newNeighbors, SortedSet<PeerAddress> queueToAsk,
            ICollection<PeerAddress> alreadyAsked, int maxNoNewInfo)
        {
            bool newInformation = Merge(queueToAsk, newNeighbors, alreadyAsked);
            if (newInformation)
            {
                _nrNoNewInfo = 0;
                return false;
            }
            return (++_nrNoNewInfo) >= maxNoNewInfo;
        }

        /// <summary>
        /// Updates queueToAsk with new data, returns if we found peers closer than we already know.
        /// </summary>
        /// <param name="queueToAsk">The queue to get updated.</param>
        /// <param name="newPeers">The new peers reported from remote peers. Since the remote peers
        /// do not know what we know, we need to filter this information.</param>
        /// <param name="alreadyAsked">The peers we already know.</param>
        /// <returns>True, if we added peers that are closer to the target than we already knew.
        /// Please note, it will return false if we add new peers that are not closer to a target.</returns>
        private static bool Merge(SortedSet<PeerAddress> queueToAsk, ICollection<PeerAddress> newPeers,
            ICollection<PeerAddress> alreadyAsked)
        {
            var result = new SortedSet<PeerAddress>(queueToAsk.Comparer);
            Utils.Utils.Difference(newPeers, result, alreadyAsked);
            if (result.Count == 0)
            {
                return false;
            }
            var first = result.Min;
            var isNewInfo = IsNew(queueToAsk, first);
            queueToAsk.AddAll(result);
            return isNewInfo;
        }

        /// <summary>
        /// Checks if an item will be the highest in a sorted set.
        /// </summary>
        /// <param name="queueToAsk">The sorted set to check.</param>
        /// <param name="item">The element to check if it will be the highest in the sorted set.</param>
        /// <returns>True, if item will be the highest element.</returns>
        private static bool IsNew(SortedSet<PeerAddress> queueToAsk, PeerAddress item)
        {
            // .NET-specific
            return queueToAsk.Min.Equals(item); // TODO works?
        }
    }
}
