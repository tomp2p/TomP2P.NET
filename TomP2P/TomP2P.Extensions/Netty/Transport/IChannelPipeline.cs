using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Netty.Transport
{
    public interface IChannelPipeline
    {
        /// <summary>
        /// Returns the list of the handler names.
        /// </summary>
        /// <returns></returns>
        IList<string> Names();

        /// <summary>
        /// Replaces the IChannelHandler of the specified name with a new handler in this pipeline.
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <param name="newHandler"></param>
        /// <returns></returns>
        IChannelHandler Replace(string oldName, string newName, IChannelHandler newHandler);

        /// <summary>
        /// Inserts a IChannelHandler at the first position of this pipeline.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        IChannelPipeline AddFirst(string name, IChannelHandler handler);

        /// <summary>
        /// Inserts a IChannelHandler before an existing handler of this pipeline.
        /// </summary>
        /// <param name="baseName"></param>
        /// <param name="name"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        IChannelPipeline AddBefore(string baseName, string name, IChannelHandler handler);
    }
}
