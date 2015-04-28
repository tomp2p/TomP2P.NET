using System.Collections.Generic;
using System.Threading.Tasks;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Core.Storage;

namespace TomP2P.Dht
{
    // TODO move same properties of Put/Get/Digest to base class
    public class TcsGet : TcsDht
    {
        // The minimum number of expected results.
        private readonly int _min;

        // Since we receive multiple results, we have an evaluation scheme to
        // simplify the result.
        private readonly IEvaluationSchemeDht _evaluationScheme;

        // storage of results
        private IDictionary<PeerAddress, IDictionary<Number640, Data>> _rawData;
        private IDictionary<PeerAddress, DigestResult> _rawDigest;
        private IDictionary<PeerAddress, byte> _rawStatus;

        // flag indicating if the minimum operations for GET have been reached
        private bool _minReached;

        public TcsGet(DhtBuilder<dynamic> builder)
            : this(builder, 0, new VotingSchemeDht())
        { }

        public TcsGet(DhtBuilder<dynamic> builder, int min, IEvaluationSchemeDht evaluationScheme)
            : base(builder)
        {
            _min = min;
            _evaluationScheme = evaluationScheme;
        }

        /// <summary>
        /// Finishes this task and sets the keys and data that has been received.
        /// </summary>
        /// <param name="rawData">The keys and adata that have been received with information from which peer.</param>
        /// <param name="rawDigest">The hashes of the content stored with information from which peer.</param>
        /// <param name="rawStatus"></param>
        /// <param name="tasksCompleted"></param>
        public void SetReceivedData(IDictionary<PeerAddress, IDictionary<Number640, Data>> rawData,
            IDictionary<PeerAddress, DigestResult> rawDigest, IDictionary<PeerAddress, byte> rawStatus,
            Task tasksCompleted)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return;
                }
                _rawData = rawData;
                _rawDigest = rawDigest;
                _rawStatus = rawStatus;
                TasksCompleted = tasksCompleted;
                var size = rawStatus.Count;
                _minReached = size >= _min;
                // TODO type and reason needed?
            }
            NotifyListeners();
        }

        /// <summary>
        /// The raw data from the GET operation.
        /// </summary>
        public IDictionary<PeerAddress, IDictionary<Number640, Data>> RawData
        {
            get
            {
                lock (Lock)
                {
                    return _rawData;
                }
            }
        }

        /// <summary>
        /// The raw digest information with hashes of the content and the 
        /// information which peer has bee contacted.
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
        /// The raw status information.
        /// </summary>
        public IDictionary<PeerAddress, byte> RawStatus
        {
            get
            {
                lock (Lock)
                {
                    return _rawStatus;
                }
            }
        }

        /// <summary>
        /// The digest information from the GET after evaluation.
        /// The evaluation gets rid of the peer address information.
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
        /// The data from GET after evaluation.
        /// The evaluation gets rid of the peer address information.
        /// </summary>
        public IDictionary<Number640, Data> DataMap
        {
            get
            {
                lock (Lock)
                {
                    return _evaluationScheme.Evaluate2(_rawData);
                }
            }
        }

        /// <summary>
        /// The first data object from GET after evaluation.
        /// </summary>
        public Data Data
        {
            get
            {
                var dataMap = DataMap;
                if (dataMap.Count == 0)
                {
                    return null;
                }
                return dataMap.Values.GetEnumerator().Current; // TODO check if correct
            }
        }

        /// <summary>
        /// Indicates if the minimum of expected results have been reached.
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

        public bool IsEmpty
        {
            get
            {
                lock (Lock)
                {
                    foreach (var byteVal in _rawStatus.Values)
                    {
                        if (byteVal == (int) PutStatus.Ok) // TODO check if works
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
        }
    }
}
