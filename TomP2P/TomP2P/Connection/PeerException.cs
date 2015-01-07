using System;
using System.Threading.Tasks;
using TomP2P.Futures;

namespace TomP2P.Connection
{
    /// <summary>
    /// This exception is used internally and passed over to the method ExceptionCaught().
    /// A PeerException always has a cause.
    /// </summary>
    public class PeerException : Exception
    {
        /// <summary>
        /// The cause of the exception.
        /// </summary>
        public AbortCauseEnum AbortCause { get; private set; }

        public enum AbortCauseEnum
        {
            /// <summary>
            /// Means that this peer aborts the communication.
            /// </summary>
            UserAbort, 
            /// <summary>
            /// Means that the other peer did not react as expected (e.g., no reply).
            /// </summary>
            PeerError, 
            /// <summary>
            /// Means that the other peer found an error on our side (e.g., if this peer thinks the other peer is someone else).
            /// </summary>
            PeerAbort, 
            Timeout, 
            Shutdown, 
            ProbablyOffline
        }

        /// <summary>
        /// Specified error with custom message.
        /// </summary>
        /// <param name="abortCauseEnum">Either USER_ABORT, PEER_ERROR, PEER_ABORT, or TIMEOUT.</param>
        /// <param name="message">Custom message.</param>
        public PeerException(AbortCauseEnum abortCauseEnum, String message)
            : base(message)
        {
            AbortCause = abortCauseEnum;
        }

        public PeerException(TaskCompletionSource<Message.Message> tcs)
            : base(tcs.Task.Exception.Message) // TODO find safer way (although, so far only invoked OnFaulted)
        {
            AbortCause = AbortCauseEnum.PeerError;
        }

        public PeerException(Exception cause)
            : base("inner exception", cause)
        {
            AbortCause = AbortCauseEnum.PeerError;
        }

        public override string ToString()
        {
            return "PeerException (" + AbortCause + "): " + Message;
        }
    }
}
