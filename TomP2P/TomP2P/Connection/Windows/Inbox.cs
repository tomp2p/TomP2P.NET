using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Class that handles incoming messages.
    /// (Somewhat an equivalent to Java Netty's SimpleChannelInboundHandler class.)
    /// </summary>
    public abstract class Inbox // TODO maybe rename, needed at all?
    {
        public abstract void MessageReceived(Message.Message message);

        public virtual void ExceptionCaught(Exception cause)
        {
            throw new NotImplementedException();
        }
    }
}
