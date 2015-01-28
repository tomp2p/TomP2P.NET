using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions.Netty;

namespace TomP2P.Connection
{
    /// <summary>
    /// A factory that creates timeout handlers.
    /// </summary>
    public class TimeoutFactory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly TaskCompletionSource<Message.Message> _tcsResponse;
        private readonly int _timeoutSeconds;
        private readonly IList<IPeerStatusListener> _peerStatusListeners;
        private readonly string _name;

        /// <summary>
        /// Creates a factory for timeout handlers.
        /// </summary>
        /// <param name="tcsResponse">The TCS for the response message. (FutureResponse equivalent.)</param>
        /// <param name="timeoutSeconds">The time for a timeout.</param>
        /// <param name="peerStatusListeners">The listeners that get notified when a timeout happens.</param>
        /// <param name="name"></param>
        public TimeoutFactory(TaskCompletionSource<Message.Message> tcsResponse, int timeoutSeconds,
            IList<IPeerStatusListener> peerStatusListeners, string name)
        {
            _tcsResponse = tcsResponse;
            _timeoutSeconds = timeoutSeconds;
            _peerStatusListeners = peerStatusListeners;
            _name = name;
        }

        public IChannelHandler CreateIdleStateHandlerTomP2P()
        {
            throw new NotImplementedException();
        }

        public IChannelHandler CreateTimeHandler()
        {
            throw new NotImplementedException();
        }

        public static void RemoveTimeout()
        {
            // TODO implement
            throw new NotImplementedException();
        }
    }
}
