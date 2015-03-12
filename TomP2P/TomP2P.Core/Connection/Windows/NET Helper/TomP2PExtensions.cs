using System.Threading.Tasks;
using TomP2P.Core.Connection.Windows.Netty;

namespace TomP2P.Core.Connection.Windows
{
    public static class TomP2PExtensions
    {
        /// <summary>
        /// Set the result as soon as the channel is closed.
        /// </summary>
        /// <param name="tcs"></param>
        /// <param name="result"></param>
        /// <param name="ctx"></param>
        public static void ResponseLater(this TaskCompletionSource<Message.Message> tcs, Message.Message result, ChannelHandlerContext ctx)
        {
            ctx.Channel.Closed += channel => tcs.SetResult(result);
        }
    }
}
