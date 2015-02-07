using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Futures
{
    public class TaskForkJoin<TTask> : TaskCompletionSource<object> where TTask : Task
    {
        private readonly VolatileReferenceArray<TTask> _forks;

        private readonly int _nrTasks;

        private readonly int _nrFinishTaskSuccess;

        private readonly bool _cancelTasksOnFinish;

        private readonly IList<TTask> _forksCopy = new List<TTask>();

        // all these values are accessed within synchronized blocks
        private int _counter = 0;
        private int _successCounter = 0;

        /// <summary>
        /// Facade if we expect everythin to return successfully.
        /// </summary>
        /// <param name="forks">The tasks that can also be modified outside this class.
        /// If a task is finished, the the task in that array will be set to null.
        /// A task may be initially null, which is considered a failure.</param>
        public TaskForkJoin(VolatileReferenceArray<TTask> forks)
            : this(forks.Length, false, forks)
        { }

        public TaskForkJoin(int nrFinishFuturesSuccess, bool cancelTasksOnFinish, VolatileReferenceArray<TTask> forks)
        {
            
        }
    }
}
