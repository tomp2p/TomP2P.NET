using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TomP2P.Futures
{
    /// <summary>
    /// Each response has one request message. The corresponding response message
    /// is set only if the request has been successful. This is indicated with
    /// IsFailed.
    /// </summary>
    public class FutureResponse
    {
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
    }
}
