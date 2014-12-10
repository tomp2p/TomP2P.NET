using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TomP2P.Extensions.Netty.Transport;
using TomP2P.Futures;

namespace TomP2P.Connection
{
    /// <summary>
    /// Creates the channels. This class is created by ConnectionReservation and should never be called directly.
    /// With this class one can create TCP or UDP channels up to a certain extent. Thus it must be known beforehand
    /// how much creations will be created.
    /// </summary>
    public class ChannelCreator
    {
        /// <summary>
        /// Setup the close listener for a channel that was already created.
        /// </summary>
        /// <param name="channelFuture"></param>
        /// <param name="futureResponse"></param>
        public void SetupCloseListener(IChannelFuture channelFuture, FutureResponse futureResponse)
        {
            throw new NotImplementedException();
        }
    }
}
