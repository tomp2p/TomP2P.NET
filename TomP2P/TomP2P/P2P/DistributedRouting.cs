using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;
using TomP2P.Futures;
using TomP2P.P2P.Builder;
using TomP2P.Peers;
using TomP2P.Rpc;
using TomP2P.Utils;

namespace TomP2P.P2P
{
    // TODO Java: add timing constraints for the routing. This would allow for slow routing requests to have a chance to repor the neighbors.

    /// <summary>
    /// Handles the routing of nodes to other nodes.
    /// </summary>
    public class DistributedRouting
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly NeighborRpc _neighbors;
        private readonly PeerBean _peerBean;
        private readonly Random _rnd;

        public DistributedRouting(PeerBean peerBean, NeighborRpc neighbors)
        {
            _neighbors = neighbors;
            _peerBean = peerBean;
            // stable random number: no need to be truly random
            _rnd = new Random(peerBean.ServerPeerAddress.PeerId.GetHashCode());
        }

        /// <summary>
        /// Bootstraps to the given peer addresses. I.e, looking for near nodes.
        /// </summary>
        /// <param name="peerAddresses">The node to which bootstrap should be performed to.</param>
        /// <param name="routingBuilder">All relevant information for the routing process.</param>
        /// <param name="channelCreator">The channel creator.</param>
        /// <returns>A task object that is set to complete if the route has been found.</returns>
        public Task<Pair<TcsRouting, TcsRouting>> Bootstrap(ICollection<PeerAddress> peerAddresses,
            RoutingBuilder routingBuilder, ChannelCreator cc)
        {
            // search close peers
            Logger.Debug("Broadcast to {0}.", peerAddresses);
            var taskDone = new TaskCompletionSource<Pair<TcsRouting, TcsRouting>>();

            // first, we find close peers to us
            routingBuilder.IsBootstrap = true;

            var taskRouting = Routing(peerAddresses, routingBuilder, Message.Message.MessageType.Request1, cc);
            // we need to know other peers as well
            // this is important if this peer is passive and only replies on requests from other peers
            taskRouting.
        }

        /// <summary>
        /// Looks for a route to the given peer address.
        /// </summary>
        /// <param name="peerAddresses">Nodes that should be asked first for a route.</param>
        /// <param name="routingBuilder"></param>
        /// <param name="type"></param>
        /// <param name="cc"></param>
        /// <returns>A task object that is set to complete if the route has been found.</returns>
        private TcsRouting Routing(ICollection<PeerAddress> peerAddresses, RoutingBuilder routingBuilder,
            Message.Message.MessageType type, ChannelCreator cc)
        {
            if (peerAddresses == null)
            {
                throw new ArgumentException("Some nodes/addresses need to be specified.");
            }
            bool randomSearch = routingBuilder.LocationKey == null;
            IComparer<PeerAddress> comparer;
            if (randomSearch)
            {
                comparer = _peerBean.PeerMap.CreateComparer();
            }
            else
            {
                comparer = PeerMap.CreateComparer(routingBuilder.LocationKey);
            }
            var queueToAsk = new SortedSet<PeerAddress>(comparer);
            var alreadyAsked = new SortedSet<PeerAddress>(comparer);

            // As presented by Kazuyuki Shudo at AIMS 2009, it is better to ask random
            // peers with the data than ask peers that are ordered by distance.
            // -> this balances load
            var directHits = new SortedDictionary<PeerAddress, DigestInfo>(_peerBean.PeerMap.CreateComparer());
            var potentialHits = new SortedSet<PeerAddress>(comparer);

            // fill initially
            queueToAsk.AddAll(peerAddresses);
            alreadyAsked.Add(_peerBean.ServerPeerAddress);
            potentialHits.Add(_peerBean.ServerPeerAddress);

            // domain key can be null if we bootstrap
            if (type == Message.Message.MessageType.Request2
                && routingBuilder.DomainKey != null
                && !randomSearch
                && _peerBean.DigestStorage != null)
            {
                Number640 from;
                Number640 to;
                if (routingBuilder.From != null && routingBuilder.To != null)
                {
                    from = routingBuilder.From;
                    to = routingBuilder.To;
                }
                else if (routingBuilder.DomainKey == null)
                {
                    from = new Number640(routingBuilder.LocationKey, Number160.Zero, Number160.Zero, Number160.Zero);
                    to = new Number640(routingBuilder.LocationKey, Number160.MaxValue, Number160.MaxValue, Number160.MaxValue);
                }
                else if (routingBuilder.ContentKey == null)
                {
                    from = new Number640(routingBuilder.LocationKey, routingBuilder.DomainKey, Number160.Zero, Number160.Zero);
                    to = new Number640(routingBuilder.LocationKey, routingBuilder.DomainKey, Number160.MaxValue, Number160.MaxValue);
                }
                else
                {
                    from = new Number640(routingBuilder.LocationKey, routingBuilder.DomainKey, routingBuilder.ContentKey, Number160.Zero);
                    to = new Number640(routingBuilder.LocationKey, routingBuilder.DomainKey, routingBuilder.ContentKey, Number160.MaxValue);
                }

                var digestBean = _peerBean.DigestStorage.Digest(from, to, -1, true);
                if (digestBean.Size > 0)
                {
                    directHits.Add(_peerBean.ServerPeerAddress, digestBean);
                }
            }
            else if (type == Message.Message.MessageType.Request3
                     && !randomSearch
                     && _peerBean.DigestTracker != null)
            {
                var digestInfo = _peerBean.DigestTracker.Digest(routingBuilder.LocationKey, routingBuilder.DomainKey,
                    routingBuilder.ContentKey);
                // we always put ourselfs to the tracker list, so we need to check
                // if we know also other peers on our trackers
                if (digestInfo.Size > 0)
                {
                    directHits.Add(_peerBean.ServerPeerAddress, digestInfo);
                }
            }

            var tcsRouting = new TcsRouting();
            if (peerAddresses.Count == 0)
            {
                tcsRouting.SetNeighbors(directHits, potentialHits, alreadyAsked, routingBuilder.IsBootstrap, false);
            }
            else
            {
                // If a peer bootstraps to itself, then the size of peer addresses is 1
                // and it contains itself. Check for that because we need to know if we
                // are routing, bootstrapping and bootstrapping to ourselfs, to return
                // the correct status for the task.
                bool isRoutingOnlyToSelf = peerAddresses.Count == 1 &&
                                           peerAddresses.First().Equals(_peerBean.ServerPeerAddress);

                var routingMechanism = routingBuilder.CreateRoutingMechanism(tcsRouting);
                routingMechanism.SetQueueToAsk(queueToAsk);
                routingMechanism.SetPotentialHits(potentialHits);
                routingMechanism.SetDirectHits(directHits);
                routingMechanism.SetAlreadyAsked(alreadyAsked);

                routingBuilder.SetIsRoutingOnlyToSelf(isRoutingOnlyToSelf);
                RoutingRec(routingBuilder, routingMechanism, type, cc);
            }

            return tcsRouting;
        }

        /// <summary>
        /// Looks for a route to the 
        /// </summary>
        /// <param name="routingBuilder"></param>
        /// <param name="routingMechanism"></param>
        /// <param name="type"></param>
        /// <param name="channelCreator"></param>
        private void RoutingRec(RoutingBuilder routingBuilder, RoutingMechanism routingMechanism,
            Message.Message.MessageType type, ChannelCreator channelCreator)
        {
            bool randomSearch = routingBuilder.LocationKey == null;
            int active = 0;
            for (int i = 0; i < routingMechanism.Parallel; i++)
            {
                if (routingMechanism.TcsResponse(i) == null
                    && !routingMechanism.IsStopCreatingNewFutures)
                {
                    PeerAddress next;
                    if (randomSearch)
                    {
                        next = routingMechanism.PollRandomInQueueToAsk(_rnd);
                    }
                    else
                    {
                        next = routingMechanism.PollFirstInQueueToAsk();
                    }
                    if (next != null)
                    {
                        routingMechanism.AddToAlreadyAsked(next);
                        active++;
                        // If we search for a random peer, then the peer should
                        // return the address farest away.
                        var locationKey2 = randomSearch
                            ? next.PeerId.Xor(Number160.MaxValue)
                            : routingBuilder.LocationKey;
                        routingBuilder.LocationKey = locationKey2;

                        // routing is per default UDP, don't show warning if the other TCP/UDP is used
                        // TODO find .NET-specific way to show sanity check warning

                        routingMechanism.TcsResponse(i,
                            _neighbors.CloseNeighbors(next, routingBuilder.SearchValues(), type, channelCreator,
                                routingBuilder));
                        Logger.Debug("Get close neighbours: {0} on {1}.", next, i);
                    }
                }
                else if (routingMechanism.TcsResponse(i) != null)
                {
                    Logger.Debug("Activity on {0}.", i);
                    active++;
                }
            }

            if (active == 0)
            {
                Logger.Debug("No activity, closing.");
                routingMechanism.SetNeighbors(routingBuilder);
                routingMechanism.Cancel();
                return;
            }

            // .NET-specific: // TODO move to TcsForkJoin as separate c'tor?
            var extractedTasks = new Task<Message.Message>[routingMechanism.TcsResponses.Length];
            for (int i = 0; i < routingMechanism.TcsResponses.Length; i++)
            {
                extractedTasks[i] = routingMechanism.TcsResponse(i).Task;
            }
            var volatileArray = new VolatileReferenceArray<Task<Message.Message>>(extractedTasks);

            // TODO works?
            bool last = active == 1;
            var tcsForkJoin = new TcsForkJoin<Task<Message.Message>>(1, false, volatileArray);
            tcsForkJoin.Task.ContinueWith(tfj =>
            {
                bool finished;
                if (!tfj.IsFaulted)
                {
                    var lastResponse = tcsForkJoin.Last.Result;
                    var remotePeer = lastResponse.Sender;
                    routingMechanism.AddPotentialHits(remotePeer);
                    var newNeighbors = lastResponse.NeighborsSet(0).Neighbors;

                    var resultSize = lastResponse.IntAt(0);
                    var keyDigest = lastResponse.Key(0);
                    var contentDigest = lastResponse.Key(1);
                    var digestBean = new DigestInfo(keyDigest, contentDigest, resultSize);
                    Logger.Debug("Peer ({0}) {1} reported {2} in message {3}.", (digestBean.Size > 0 ? "direct" : "none"), remotePeer, newNeighbors, lastResponse);
                    finished = routingMechanism.EvaluateSuccess(remotePeer, digestBean, newNeighbors, last,
                        routingBuilder.LocationKey);
                    Logger.Debug("Routing finished {0} / {1}.", finished, routingMechanism.IsStopCreatingNewFutures);
                }
                else
                {
                    // if it failed but the failed is the closest one, it is good to try again,
                    // since the peer might just be busy
                    Logger.Debug("Routing error {0}.", tfj.Exception);
                    finished = routingMechanism.EvaluateFailed();
                    routingMechanism.IsStopCreatingNewFutures = finished;
                }

                if (finished)
                {
                    Logger.Debug("Routing finished. Direct hits: {0}. Potential hits: {1}.", routingMechanism.DirectHits, routingMechanism.PotentialHits);
                    routingMechanism.SetNeighbors(routingBuilder);
                    routingMechanism.Cancel();
                    // stop all operations, as we are finished, no need to go further
                }
                else
                {
                    RoutingRec(routingBuilder, routingMechanism, type, channelCreator);
                }
            });

        }
    }
}
