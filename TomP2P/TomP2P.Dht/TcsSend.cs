using System.Collections.Generic;
using System.Threading.Tasks;
using TomP2P.Core.Peers;
using TomP2P.Extensions.Netty.Buffer;

namespace TomP2P.Dht
{
    public class TcsSend : TcsDht
    {
        // The minimum number of expected results.
        private readonly int _min;

        // Since we receive multiple results, we have an evaluation scheme to
        // simplify the result.
        private readonly IEvaluationSchemeDht _evaluationScheme;

        // storage of results
        private IDictionary<PeerAddress, ByteBuf> _rawChannels;
        private IDictionary<PeerAddress, object> _rawObjects;

        // flag indicating if the minimum operations for GET have been reached
        private bool _minReached;

        public TcsSend(DhtBuilder<dynamic> builder)
            : this(builder, 0, new VotingSchemeDht())
        { }

        public TcsSend(DhtBuilder<dynamic> builder, int min, IEvaluationSchemeDht evaluationScheme)
            : base(builder)
        {
            _min = min;
            _evaluationScheme = evaluationScheme;
        }

        /// <summary>
        /// Finishes the task and sets the keys and data that has been sent directly using the Netty buffer.
        /// </summary>
        /// <param name="rawChannels">The raw data that has been sent directly with information from which peer.</param>
        /// <param name="tasksCompleted"></param>
        public void SetDirectData1(IDictionary<PeerAddress, ByteBuf> rawChannels, Task tasksCompleted)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return;
                }
                _rawChannels = rawChannels;
                TasksCompleted = tasksCompleted;
                var size = _rawChannels.Count;
                _minReached = size >= _min;
                // TODO type and reason needed?
            }
        }

        /// <summary>
        /// Finishes the task and sets the keys and data that has been sent directly using an object.
        /// </summary>
        /// <param name="rawObjects">The object that has been sent directly with information from which peer.</param>
        /// <param name="tasksCompleted"></param>
        public void SetDirectData2(IDictionary<PeerAddress, object> rawObjects, Task tasksCompleted)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return;
                }
                _rawObjects = rawObjects;
                TasksCompleted = tasksCompleted;
                var size = rawObjects.Count;
                _minReached = size >= _min;
                // TODO type and reason needed?
            }
            NotifyListeners();
        }

        /// <summary>
        /// The raw data from SendDirect(). (ByteBuf)
        /// </summary>
        public IDictionary<PeerAddress, ByteBuf> RawDirectData1
        {
            get
            {
                lock (Lock)
                {
                    return _rawChannels;
                }
            }
        }

        /// <summary>
        /// The raw data from SendDirect(). (object)
        /// </summary>
        public IDictionary<PeerAddress, object> RawDirectData2
        {
            get
            {
                lock (Lock)
                {
                    return _rawObjects;
                }
            }
        }

        /// <summary>
        /// The data from SendDirect() after evaluation. (ByteBuf)
        /// e evaluation gets rid of the peer address information.
        /// </summary>
        public ByteBuf ChannelBuffer
        {
            get
            {
                lock (Lock)
                {
                    return _evaluationScheme.Evaluate4(_rawChannels);
                }
            }
        }

        /// <summary>
        /// The data from SendDirect() after evaluation. (object)
        /// e evaluation gets rid of the peer address information.
        /// </summary>
        public object Object
        {
            get
            {
                lock (Lock)
                {
                    return _evaluationScheme.Evaluate3(_rawObjects);
                }
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
    }
}
