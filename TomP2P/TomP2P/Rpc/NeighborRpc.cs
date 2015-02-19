using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Peers;

namespace TomP2P.Rpc
{
    /// <summary>
    /// Handles the neighbor requests and replies.
    /// </summary>
    public class NeighborRpc
    {
        // TODO implement NeighborRpc

        public Task<Message.Message> CloseNeighbors(PeerAddress remotePeer, SearchValues searchValues, Message.Message.MessageType type,
            ChannelCreator channelCreator, IConnectionConfiguration configuration)
        {
            var tcsResponse = CloseNeighborsTcs(remotePeer, searchValues, type, channelCreator, configuration);
            return tcsResponse.Task;
        }

        /// <summary>
        /// .NET-specific: Used for DistributedRouting only.
        /// </summary>
        internal TaskCompletionSource<Message.Message> CloseNeighborsTcs(PeerAddress remotePeer, SearchValues searchValues, Message.Message.MessageType type,
            ChannelCreator channelCreator, IConnectionConfiguration configuration)
        {
            throw new NotImplementedException();
        }
    }
}
