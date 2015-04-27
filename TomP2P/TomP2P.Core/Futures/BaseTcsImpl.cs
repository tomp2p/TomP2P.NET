using System.Threading.Tasks;

namespace TomP2P.Core.Futures
{
    /// <summary>
    /// Equivalent to Java's BaseFutureImpl. Only required members for this project
    /// are implemented.
    /// </summary>
    public abstract class BaseTcsImpl : TaskCompletionSource<object>, IBaseTcs
    {
        protected object Lock;

        protected bool Completed;

        /// <summary>
        /// Default constructor that sets the lock object, which is used 
        /// for synchronization to this instance.
        /// </summary>
        protected BaseTcsImpl()
        {
            Lock = new object();
        }

        protected bool CompletedAndNotify()
        {
            if (!Completed)
            {
                Completed = true;
                // lock.notifyAll()
                return true;
            }
            return false;
        }

        protected void NotifyListeners()
        {
            // in .NET, this just means to set the result
            SetResult(null);
        }
    }
}
