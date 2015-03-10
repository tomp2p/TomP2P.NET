namespace TomP2P.Extensions.Netty.Buffer
{
    public sealed class Unpooled
    {
        private static readonly IByteBufAllocator Alloc = UnpooledByteBufAllocator.Default;
        
        /// <summary>
        /// A buffer whose capacity is 0.
        /// </summary>
        public static readonly ByteBuf EmptyBuffer = new EmptyByteBuf(); // Alloc.Buffer(0, 0);

        /// <summary>
        /// Creates a new big-endian Java heap buffer with the specified capacity, which
        /// expands its capacity boundlessly on demand. The new buffer's ReaderIndex and
        /// WriterIndex are 0.
        /// </summary>
        /// <param name="initialCapacity"></param>
        /// <returns></returns>
        public static ByteBuf Buffer(int initialCapacity)
        {
            return Alloc.HeapBuffer(initialCapacity);
        }

        public static CompositeByteBuf CompositeBuffer()
        {
            return CompositeBuffer(16);
        }

        public static CompositeByteBuf CompositeBuffer(int maxNumComponents)
        {
            return new CompositeByteBuf(Alloc, false, maxNumComponents);
        }

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
        /// Creates a new big-endian buffer which wraps the specified array.
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static ByteBuf WrappedBuffer(sbyte[] array)
        {
            if (array.Length == 0)
            {
                return EmptyBuffer;
            }
            return new UnpooledHeapByteBuf(Alloc, array, array.Length);
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
            if (length == 0)
            {
                return EmptyBuffer;
            }

            if (offset == 0 && length == array.Length)
            {
                return WrappedBuffer(array);
            }
            return WrappedBuffer(array).Slice(offset, length);
        }
    }
}
