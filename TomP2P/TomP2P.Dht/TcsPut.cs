using System.Collections.Generic;
using System.Threading.Tasks;
using TomP2P.Core.Peers;

namespace TomP2P.Dht
{
    /// <summary>
    /// The task object for PUT operations, including routing.
    /// </summary>
    public class TcsPut : TcsDht
    {
        // The minimum number of expected results. This is also used for PUT
        // operations to decide if a task failed or not.
        private readonly int _min;
        private readonly int _dataSize;

        // storage of results
        private IDictionary<PeerAddress, IDictionary<Number640, byte>> _rawResult;

        // flag indicating if the minimum operations for PUT have been reached
        private bool _minReached;

        private IDictionary<Number640, int?> _result;

        /// <summary>
        /// Creates a new DHT task object that keeps track of the status of the PUT operation.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="min">The minimum of expected results.</param>
        /// <param name="dataSize"></param>
        public TcsPut(DhtBuilder<dynamic> builder, int min, int dataSize)
            : base(builder)
        {
            _min = min;
            _dataSize = dataSize;
        }

        /// <summary>
        /// Finishes the task and sets the keys that have been stored. Success or failure is determined if the communication
        /// was successful. This means that we need to further check if the other peers have denied the storage (e.g., due to
        /// no storage space, no security permissions). Further evaluation can be retrieved with AavgStoredKeys() or if the 
        /// evaluation should be done by the user, use RawKeys().
        /// </summary>
        /// <param name="rawResult">The keys that have been stored with information on which peer it has been stored.</param>
        /// <param name="tasksCompleted"></param>
        public void SetStoredKeys(IDictionary<PeerAddress, IDictionary<Number640, byte>> rawResult, Task tasksCompleted)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return;
                }
                _rawResult = rawResult;
                var size = rawResult == null ? 0 : rawResult.Count;
                _minReached = size > _min;
                _tasksCompleted = tasksCompleted;
                // TODO type and reason needed?
            }
            NotifyListeners();
        }

        /// <summary>
        /// The average keys received from the DHT. Only evaluates rawKeys.
        /// </summary>
        public double AvgStoredKeys
        {
            get
            {
                lock (Lock)
                {
                    var size = _rawResult.Count;
                    var total = 0;
                    foreach (var dictionary in _rawResult.Values)
                    {
                        total += dictionary.Keys.Count;
                    }
                    return total/(double) size;
                }
            }
        }

        /// <summary>
        /// The raw result from the storage or removal operation.
        /// </summary>
        public IDictionary<PeerAddress, IDictionary<Number640, byte>> RawResult
        {
            get
            {
                lock (Lock)
                {
                    return _rawResult;
                }
            }
        }

        /// <summary>
        /// Checks if expected minimum results have been reached.
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

        /// <summary>
        /// The keys that have been stored or removed after evaluation. The evaluation gets rid of the peer address
        /// information, by either a majority vote or cumulation. Use EvalKeys() instead of this method.
        /// </summary>
        public IDictionary<Number640, int?> Result
        {
            get
            {
                lock (Lock)
                {
                    if (_result == null)
                    {
                        _result = Evaluate(_rawResult);
                    }
                    return _result;
                }
            }
        }

        private IDictionary<Number640, int?> Evaluate(IDictionary<PeerAddress, IDictionary<Number640, byte>> rawResult2)
        {
            var result = new Dictionary<Number640, int?>();
            foreach (var dictionary in rawResult2.Values)
            {
                foreach (var kvp in dictionary)
                {
                    if (kvp.Value == (int) PutStatus.Ok // TODO check byte -> int conversion
                        || kvp.Value == (int) PutStatus.OkPrepared
                        || kvp.Value == (int) PutStatus.OkUnchanged
                        || kvp.Value == (int) PutStatus.VersionFork
                        || kvp.Value == (int) PutStatus.Deleted)
                    {
                        var integer = result[kvp.Key];
                        if (integer == null)
                        {
                            result.Add(kvp.Key, 1);
                        }
                        else
                        {
                            result.Add(kvp.Key, integer + 1);
                        }
                    }
                }
            }
            return result;
        }

        public bool IsSuccess
        {
            // TODO move to base class (2x)
            get
            {
                if (!Task.IsFaulted)
                {
                    return false;
                }
                return CheckResults();
            }
        }

        public bool IsSuccessPartially
        {
            get
            {
                // TODO check for correctness
                var networkSuccess = Task.IsFaulted;
                return networkSuccess && Result.Count > 0;
            }
        }

        private bool CheckResults()
        {
            var res = Result;
            foreach (var kvp in res)
            {
                if (kvp.Value != _rawResult.Count)
                {
                    return false;
                }
            }
            // we know exactly how much data we need to store
            return res.Count == _dataSize;
        }
    }
}
