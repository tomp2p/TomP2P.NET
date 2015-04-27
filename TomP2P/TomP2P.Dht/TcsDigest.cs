using System.Collections.Generic;
using System.Threading.Tasks;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;

namespace TomP2P.Dht
{
    public class TcsDigest : TcsDht
    {
        // The minimum number of expected results.
        private readonly int _min;

        // Since we receive multiple results, we have an evaluation scheme
        // to simplify the result.
        private readonly IEvaluationSchemeDht _evaluationScheme;

        // digest results
        private IDictionary<PeerAddress, DigestResult> _rawDigest;
 
        // flag indicating if the minimum operations for PUT have been reached.
        private bool _minReached;

        public TcsDigest(DhtBuilder<dynamic> builder)
            : this(builder, 0, new VotingSchemeDht())
        { }

        /// <summary>
        /// Creates a new DHT task object that keeps track of the status of the DIGEST operation.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="min"></param>
        /// <param name="evaluationScheme"></param>
        public TcsDigest(DhtBuilder<dynamic> builder, int min, IEvaluationSchemeDht evaluationScheme)
            : base(builder)
        {
            _min = min;
            _evaluationScheme = evaluationScheme;
        }

        /// <summary>
        /// Finishes the task and sets the digest information that has been received.
        /// </summary>
        /// <param name="rawDigest">The hashes of the content stored with information from which peer it has been received.</param>
        /// <param name="tasksCompleted"></param>
        public void SetReceivedDigest(IDictionary<PeerAddress, DigestResult> rawDigest, Task tasksCompleted)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return;
                }
                _rawDigest = rawDigest;
                TasksCompleted = tasksCompleted;
                var size = _rawDigest.Count;
                _minReached = size >= _min;
                // TODO type and reason needed?
            }
            NotifyListeners();
        }

        /// <summary>
        /// The raw digest information with hashed of the content and the information which peer has been contacted.
        /// </summary>
        public IDictionary<PeerAddress, DigestResult> RawDigest
        {
            get
            {
                lock (Lock)
                {
                    return _rawDigest;
                }
            }
        }

        /// <summary>
        /// The digest information from the GET after evaluation. The evaluation gets rid of
        /// the peer address information, either by majority vote or cumulation.
        /// </summary>
        public DigestResult Digest
        {
            get
            {
                lock (Lock)
                {
                    return _evaluationScheme.Evaluate5(_rawDigest);
                }
            }
        }

        /// <summary>
        /// Indicates if the minimum of expected results has been reached.
        /// </summary>
        public bool IsMinReached
        {
            get
            {
                lock (Lock)
                {
                    return _minReached;
                }
            }
        }
    }
}
