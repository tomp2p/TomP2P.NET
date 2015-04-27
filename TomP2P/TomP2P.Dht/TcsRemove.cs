using System.Collections.Generic;
using System.Threading.Tasks;
using TomP2P.Core.Peers;
using TomP2P.Core.Storage;
using TomP2P.Extensions;

namespace TomP2P.Dht
{
    public class TcsRemove : TcsDht
    {
        // Since we receive multiple results, we have an evaluation scheme to 
        // simplify the result
        private readonly IEvaluationSchemeDht _evaluationScheme;

        // storage of results
        private IDictionary<PeerAddress, IDictionary<Number640, byte>> _rawKeys640;
        private IDictionary<PeerAddress, IDictionary<Number640, Data>> _rawData;
        private IDictionary<Number640, int?> _result; 

        public TcsRemove(DhtBuilder<dynamic> builder)
            : this(builder, new VotingSchemeDht())
        { }

        public TcsRemove(DhtBuilder<dynamic> builder, IEvaluationSchemeDht evaluationScheme)
            : base(builder)
        {
            _evaluationScheme = evaluationScheme;
        }

        /// <summary>
        /// Finishes the task and sets the keys that have been stored. Success or failure is determined if the communication
        /// was successful. This means that we need to further check if the other peers have denied the storage (e.g., due to
        /// no storage space, no security permissions). Further evaluation can be retrieved with AvgStoredKeys or if the 
        /// evaluation should be done by the user, use RawKeys}.
        /// </summary>
        /// <param name="rawKeys640">The keys that have been stored with information on which peer it has been stored.</param>
        /// <param name="tasksCompleted"></param>
        public void SetStoredKeys(IDictionary<PeerAddress, IDictionary<Number640, byte>> rawKeys640, Task tasksCompleted)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return;
                }
                _rawKeys640 = rawKeys640;
                TasksCompleted = tasksCompleted;
                //var size = rawKeys640 == null ? 0 : rawKeys640.Count;
                // TODO type and reason needed?
            }
            NotifyListeners();
        }

        /// <summary>
        /// The average keys received from the DHT. Only evaluates raw keys.
        /// </summary>
        public double AvgStoredKeys
        {
            get
            {
                lock (Lock)
                {
                    var size = _rawKeys640.Count;
                    var total = 0;
                    foreach (var dictionary in _rawKeys640.Values)
                    {
                        total += dictionary.Count;
                    }
                    return total / (double)size;
                }
            }
        }

        /// <summary>
        /// Finishes the task and sets the keys and data that have been received.
        /// </summary>
        /// <param name="rawData">The keys and data that have been received with information from which peer it has been received.</param>
        /// <param name="tasksCompleted"></param>
        public void SetReceivedData(IDictionary<PeerAddress, IDictionary<Number640, Data>> rawData, Task tasksCompleted)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return;
                }
                _rawData = rawData;
                TasksCompleted = tasksCompleted;
                //var size = rawData.Count;
                // TODO type and reason needed?
            }
            NotifyListeners();
        }

        /// <summary>
        /// The raw keys from the storage or removal operation.
        /// </summary>
        public IDictionary<PeerAddress, IDictionary<Number640, byte>> RawKeys
        {
            get
            {
                lock (Lock)
                {
                    return _rawKeys640;
                }
            }
        }

        /// <summary>
        /// The keys that have been stored or removed after evaluation. The evaluation
        /// gets rid of the peer address information, by either a majority vore or
        /// cumulation.
        /// </summary>
        public ICollection<Number640> EvalKeys
        {
            get
            {
                lock (Lock)
                {
                    return _evaluationScheme.Evaluate6(_rawKeys640);
                }
            }
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
        /// The data from Get() after evaluation. The evaluation gets rid of the peer
        /// address information, by either majority vote or cumulation.
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
        /// The keys that have been stored or removed after evaluation. The evaluation gets rid of
        /// peer address information, by either majority vote or cumulation. Use EvalKeys instead of
        /// this method.
        /// </summary>
        public IDictionary<Number640, int?> Result
        {
            get
            {
                lock (Lock)
                {
                    if (_result == null)
                    {
                        if (_rawKeys640 != null)
                        {
                            _result = Evaluate0(_rawKeys640);
                        }
                        else if (_rawData != null)
                        {
                            _result = Evaluate1(_rawData);
                        }
                        else
                        {
                            return Convenient.EmptyDictionary<Number640, int?>();
                        }
                    }
                    return _result;
                }
            }
        }

        private static IDictionary<Number640, int?> Evaluate0(IDictionary<PeerAddress, IDictionary<Number640, byte>> rawResult2)
        {
            var result = new Dictionary<Number640, int?>();
            foreach (var dictionary in rawResult2.Values)
            {
                foreach (var kvp in dictionary)
                {
                    if (kvp.Value == (int) PutStatus.Ok)
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

        private static IDictionary<Number640, int?> Evaluate1(IDictionary<PeerAddress, IDictionary<Number640, Data>> rawData)
        {
            var result = new Dictionary<Number640, int?>();
            foreach (var dictionary in rawData.Values)
            {
                foreach (var kvp in dictionary)
                {
                    // data is never null
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
            return result;
        }

        public bool IsRemoved
        {
            get
            {
                lock (Lock)
                {
                    if (_rawKeys640 != null)
                    {
                        return CheckAtLeastOneSuccess();
                    }
                    if (_rawData != null)
                    {
                        return CheckAtLeastOneSuccessData();
                    }
                }
                return false;
            }
        }

        private bool CheckAtLeastOneSuccess()
        {
            foreach (var kvp in _rawKeys640)
            {
                foreach (var kvp2 in kvp.Value)
                {
                    if (kvp2.Value == (int) PutStatus.Ok)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckAtLeastOneSuccessData()
        {
            foreach (var kvp in _rawData)
            {
                foreach (var kvp2 in kvp.Value)
                {
                    if (kvp2.Value != null && !kvp2.Value.IsEmpty)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
