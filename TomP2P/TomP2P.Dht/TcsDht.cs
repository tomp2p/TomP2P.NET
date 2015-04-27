using System.Collections.Generic;
using System.Threading.Tasks;
using TomP2P.Core.Connection;
using TomP2P.Core.Futures;
using TomP2P.Core.Message;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Dht
{
    public abstract class TcsDht : BaseTcsImpl
    {
        // Stores tasks of DHT operations. 6 is the maximum of tasks being
        // generaded, as seen in configurations (min.res + parr.diff).
        private readonly IList<Task<Message>> _requests = new List<Task<Message>>(6);

        /// <summary>
        /// Builder that contains the data we were looking for.
        /// </summary>
        public DhtBuilder<dynamic> Builder { get; private set; } // TODO check if best solution

        private TcsRouting _tcsRouting;
        private Task _tasksCompleted;

        /// <summary>
        /// Creates a new DHT task object that keeps track of the status of the DHT operations.
        /// </summary>
        /// <param name="builder"></param>
        protected TcsDht(DhtBuilder<dynamic> builder)
        {
            Builder = builder;
        }

        /// <summary>
        /// Adds a request that has been created for the DHT operations. This was created after the routing process.
        /// </summary>
        /// <param name="taskResponse">The taskResponse that has been created.</param>
        /// <returns></returns>
        public TcsDht AddRequests(Task<Message> taskResponse)
        {
            lock (Lock)
            {
                _requests.Add(taskResponse);
            }
            return this;
        }

        public void AddTcsDhtReleaseListener(ChannelCreator channelCreator)
        {
            Task.ContinueWith(tDht =>
            {
                FutureRequests.Task.ContinueWith(tcsForkJoin =>
                {
                    channelCreator.ShutdownAsync();
                });
            });
        }

        /// <summary>
        /// Returns those futures that are still running. If 6 storage futures are started at the same time and 5 of
        /// them finish, and we specified that we are fine if 5 finish, then TcsDht returns success. However, the future
        /// that may still be running is the one that stores the content to the closest peer. For testing this is not
        /// acceptable, thus after waiting for TcsDht, one needs to wait for the running futures as well.
        /// </summary>
        public TcsForkJoin<Task<Message>> FutureRequests
        {
            get
            {
                lock (Lock)
                {
                    int size = _requests.Count;
                    var taskResponses = new Task<Message>[size];
                    for (int i = 0; i < size; i++)
                    {
                        taskResponses[i] = _requests[i];
                    }
                    return new TcsForkJoin<Task<Message>>(new VolatileReferenceArray<Task<Message>>(taskResponses));
                }
            }
        }

        public void SetTcsRouting(TcsRouting tcsRouting)
        {
            
        }

        /// <summary>
        /// The TcsRouting that was used for the routing. Before the TcsDht is used, 
        /// TcsRouting has to be completed successfully.
        /// </summary>
        public TcsRouting TcsRouting
        {
            get
            {
                lock(Lock)
                {
                    return _tcsRouting;
                }
            }
            set
            {
                lock (Lock)
                {
                    _tcsRouting = value;
                }
            }
        }

        public Task TasksCompleted
        {
            protected set { _tasksCompleted = value; }
            get
            {
                lock (Lock)
                {
                    return _tasksCompleted;
                }
            }
        }
    }
}
