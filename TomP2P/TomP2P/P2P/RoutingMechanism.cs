using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions.Workaround;
using TomP2P.Futures;
using TomP2P.Peers;
using TomP2P.Rpc;

namespace TomP2P.P2P
{
    public class RoutingMechanism
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly VolatileReferenceArray<TaskCompletionSource<Message.Message>> _tcsResponses;
        public TaskRouting TaskRoutingResponse { get; private set; }
        private readonly ICollection<IPeerFilter> _peerFilters;

        private SortedSet<PeerAddress> _queueToAsk;
        private SortedSet<PeerAddress> _alreadyAsked;
        private SortedDictionary<PeerAddress, DigestInfo> _directHits;
        private SortedSet<PeerAddress> _potentialHits;

        private int _nrNoNewInfo = 0;
        private int _nrFailures = 0;
        private int _nrSuccess = 0;

        private int _maxDirectHits;
        private int _maxNoNewInfo;
        private int _maxFailures;
        private int _maxSucess;
        /// <summary>
        /// True, if we should stop crating more tasks. False, otherwise.
        /// </summary>
        public bool IsStopCreatingNewFutures { get; private set; }

        /// <summary>
        /// Creates the routing mechanism. Make sure to set the Max* fields.
        /// </summary>
        /// <param name="tcsResponses">The current task responses that are running.</param>
        /// <param name="taskRoutingResponse">The response task from this routing request.</param>
        /// <param name="peerFilters"></param>
        public RoutingMechanism(VolatileReferenceArray<TaskCompletionSource<Message.Message>> tcsResponses,
            TaskRouting taskRoutingResponse, ICollection<IPeerFilter> peerFilters)
        {
            _tcsResponses = tcsResponses;
            TaskRoutingResponse = taskRoutingResponse;
            _peerFilters = peerFilters;
        }

        /// <summary>
        /// The number of parallel requests. The number is determined by the length of the
        /// task response array.
        /// </summary>
        public int Parallel
        {
            get { return _tcsResponses.Length; }
        }

        /// <summary>
        /// Gets the TCS at position i.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public TaskCompletionSource<Message.Message> TcsResponse(int i)
        {
            return _tcsResponses.Get(i);
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
            return _tcsResponses.GetAndSet(i, tcsResponse);
        }

        // TODO implement rest
    }
}
