using System.Threading.Tasks;
using TomP2P.Core.Connection;
using TomP2P.Core.Message;
using TomP2P.Core.Peers;

namespace TomP2P.Dht
{
    /// <summary>
    /// The operations that create many RPCs.
    /// </summary>
    /// <typeparam name="T">The type of the task that takes care of all the RPC tasks.</typeparam>
    public interface IOperationMapper<in T> where T : TcsDht
    {
        /// <summary>
        /// Creates a single RPC.
        /// </summary>
        /// <param name="channelCreator">The channel creator to create an UDP or TCP channel.</param>
        /// <param name="remotePeerAddress">The address of the remote peer.</param>
        /// <returns></returns>
        Task<Message> Create(ChannelCreator channelCreator, PeerAddress remotePeerAddress);

        /// <summary>
        /// Called when the responses' overall task is finished.
        /// </summary>
        /// <param name="task">The overall task.</param>
        /// <param name="tasksCompleted"></param>
        void Response(T task, Task tasksCompleted);

        /// <summary>
        /// Called whenever a single task is finished.
        /// </summary>
        /// <param name="taskResponse"></param>
        void IntermediateResponse(Task<Message> taskResponse);
    }
}
