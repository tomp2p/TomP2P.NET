using System;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// Used for TaskResponse.(Try)SetException().
    /// Thus used to notify about a failure in the execution of the underlying Task.
    /// </summary>
    public class TaskFailedException : Exception
    {
        public TaskFailedException(string message)
            : base(message)
        { }

        public TaskFailedException(Task baseTask)
            : base(baseTask.TryGetException().Message)
        { }

        public TaskFailedException(string message, Task baseTask)
            : base(
                String.Format("{0}{1}", message, baseTask != null ? " <-> " + baseTask.TryGetException().Message : String.Empty))
        {
            
        }
    }
}
