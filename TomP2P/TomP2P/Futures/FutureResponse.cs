using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Futures
{
    /// <summary>
    /// Each response has one request message. The corresponding response message
    /// is set only if the request has been successful. This is indicated with
    /// IsFailed.
    /// </summary>
    public class FutureResponse // TODO inherit from Task
    {
        /// <summary>
        /// Creates a future and sets the request message.
        /// </summary>
        /// <param name="requestMessage">The request message that will be sent.</param>
        public FutureResponse(Message.Message requestMessage)
            : this(requestMessage, new FutureSuccessEvaluatorCommunication())
        { }

        public bool IsCompleted()
        {
            // TODO in Java, this method is implemented in BaseFutureImpl
            throw new NotImplementedException();
        }

        public bool IsFailed()
        {
            // TODO in Java, this method is implemented in BaseFutureImpl
            throw new NotImplementedException();
        }

        public String FailedReason()
        {
            // TODO in Java, this method is implemented in BaseFutureImpl
            throw new NotImplementedException();
        }

        public void Failed(string failed)
        {
            // TODO in Java, this method is implemented in BaseFutureImpl
            throw new NotImplementedException();
        }

        /// <summary>
        /// The FutureResponse always keeps a reference to the request.
        /// </summary>
        /// <returns></returns>
        public Message.Message Request()
        {
            throw new NotImplementedException();
        }
    }
}
