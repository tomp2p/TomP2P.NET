using System;

namespace TomP2P.Connection.Windows
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
    }
}
