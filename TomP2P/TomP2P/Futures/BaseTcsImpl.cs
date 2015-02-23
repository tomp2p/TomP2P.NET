using System.Threading.Tasks;

namespace TomP2P.Futures
{
    /// <summary>
    /// Equivalent to Java's BaseFutureImpl. Only required members for this project
    /// are implemented.
    /// </summary>
    public abstract class BaseTcsImpl : TaskCompletionSource<object>, IBaseTcs
    {
        protected object Lock;

        protected bool Completed = false;

        /// <summary>
        /// Default constructor that sets the lock object, which is used 
        /// for synchronization to this instance.
        /// </summary>
        protected BaseTcsImpl()
        {
            Lock = this;
        }

        protected bool CompletedAndNotify()
        {
            if (!Completed)
            {
                Completed = true;
                // lock.notifyAll()
                return true;
            }
            else
            {
                return false;
            }
        }

        protected void NotifyListeners()
        {
            // in .NET, this just means to set the result
            this.SetResult(null);
        }
    }
}
