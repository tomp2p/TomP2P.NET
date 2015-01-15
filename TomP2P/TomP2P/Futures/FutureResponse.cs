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
    public class FutureResponse : Task<Message.Message>
    {
        public FutureResponse(Func<Message.Message> function)
            : base(function)
        { }

        // a FutureResponse is actually nothing more than a Task<Message> (or TaskResponse<Message>)
        // but maybe, we need to add some functionalities for this .NET "future"
    }
}
