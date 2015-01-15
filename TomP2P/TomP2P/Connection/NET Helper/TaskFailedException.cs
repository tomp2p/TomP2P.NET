using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.NET_Helper
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
