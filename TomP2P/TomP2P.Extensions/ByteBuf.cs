using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions
{
    /// <summary>
    /// Equivalent of Java Netty's ByteBuf.
    /// </summary>
    public abstract class ByteBuf
    {
        /// <summary>
        /// Returns the number of readable bytes which is equal to WriterIndex - ReaderIndex.
        /// </summary>
        public abstract int ReadableBytes { get; }

        public abstract int WriterIndex { get; }

        /// <summary>
        /// Returns the number of bytes (octets) this buffer can contain.
        /// </summary>
        public int Capacity;

        public abstract ByteBuf Duplicate();
    }
}
