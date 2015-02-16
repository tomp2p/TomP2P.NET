using System.Collections.Generic;
using System.Threading.Tasks;
using TomP2P.Peers;

namespace TomP2P.Futures
{
    // TODO remove this class
    /// <summary>
    /// The bootstrap will be a wrapped task because we need to ping a server first.
    /// If this ping is successful, we can bootstrap.
    /// </summary>
    /// <typeparam name="TTask"></typeparam>
    public class TcsWrappedBootstrap<TTask> : TcsWrapper<TTask> where TTask : Task
    {
        private ICollection<PeerAddress> _bootsrapTo;

        /// <summary>
        /// The addresses we bootstrap to. If we broadcast, we don't know the
        /// addresses in advance.
        /// </summary>
        /// <param name="bootstrapTo">A collection of peers involved in the bootstrapping.</param>
        public void SetBootstrapTo(ICollection<PeerAddress> bootstrapTo)
        {
            lock (Lock)
            {
                _bootsrapTo = bootstrapTo;
            }
        }

        /// <summary>
        /// Returns a collection of peers that were involved in the bootstrapping.
        /// </summary>
        public ICollection<PeerAddress> BootstrapTo
        {
            get
            {
                lock (Lock)
                {
                    return _bootsrapTo;
                }
            }
        }
    }
}
