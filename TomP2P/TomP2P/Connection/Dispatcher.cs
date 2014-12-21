using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TomP2P.Connection
{
    /// <summary>
    /// Used to deliver incoming REQUEST messages to their specific handlers.
    /// Handlers can be registered using the RegisterIoHandler function.
    /// <para>
    /// You probably want to add an instance of this class to the end of a pipeline to be able to receive messages.
    /// This class is able to cover several channels but only one P2P network!
    /// </para>
    /// </summary>
    public class Dispatcher // TODO extends SimpleChannelInboundHandler<Message>
    {
        // TODO needed?
    }
}
