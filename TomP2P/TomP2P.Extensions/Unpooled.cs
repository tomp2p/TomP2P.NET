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

        private static readonly IByteBufAllocator Alloc = null; // TODO implement

        /// <summary>
        /// Creates a new big-endian composite buffer which wraps the readable bytes of the 
        /// specified buffers without copying them. A modification on the content of the 
        /// specified buffers will be visible to the returned buffer.
        /// </summary>
        /// <param name="buffers"></param>
        /// <returns></returns>
        public static ByteBuf WrappedBuffer(params ByteBuf[] buffers)
        {
            return WrappedBuffer(16, buffers);
        }

        /// <summary>
        /// Creates a new big-endian composite buffer which wraps the readable bytes of the
        /// specified buffers without copying them.  A modification on the content of the 
        /// specified buffers will be visible to the returned buffer.
        /// </summary>
        /// <param name="maxNumComponents"></param>
        /// <param name="buffers"></param>
        /// <returns></returns>
        public static ByteBuf WrappedBuffer(int maxNumComponents, params ByteBuf[] buffers)
        {
            switch (buffers.Length)
            {
                case 0:
                    break;
                case 1:
                    if (buffers[0].IsReadable)
                    {
                        return WrappedBuffer(buffers[0]); // little-endian
                    }
                    break;
                default:
                    foreach (var b in buffers)
                    {
                        if (b.IsReadable)
                        {
                            return new CompositeByteBuf(Alloc, false, maxNumComponents, buffers);
                        }
                    }
                    break;
            }
            return EmptyBuffer;
        }

        public static ByteBuf WrappedBuffer(ByteBuf buffer)
        {
            if (buffer.IsReadable)
            {
                return buffer.Slice();
            }
            else
            {
                return EmptyBuffer;
            }
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
