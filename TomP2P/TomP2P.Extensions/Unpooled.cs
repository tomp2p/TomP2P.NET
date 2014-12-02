using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    public sealed class Unpooled
    {
        public static readonly ByteBuf EmptyBuffer = null; // TODO implement

        /// <summary>
        /// Creates a new big-endian composite buffer which wraps the readable bytes of the 
        /// specified buffers without copying them. A modification on the content of the 
        /// specified buffers will be visible to the returned buffer.
        /// </summary>
        /// <param name="buffers"></param>
        /// <returns></returns>
        public static ByteBuf WrappedBuffer(params ByteBuf[] buffers)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new big-endian buffer which wraps the sub-region of the specified array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static ByteBuf WrappedBuffer(sbyte[] array, int offset, int length)
        {
            // TODO implement
            throw new NotImplementedException();
        }
    }
}
