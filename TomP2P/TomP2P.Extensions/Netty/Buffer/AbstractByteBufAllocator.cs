using System;

namespace TomP2P.Extensions.Netty
{
    public abstract class AbstractByteBufAllocator : IByteBufAllocator
    {
        //private static readonly int DEFAULT_INITIAL_CAPACITY = 256;
        //private static readonly int DEFAULT_MAX_COMPONENTS = 16;

        private readonly bool _directByDefault;
        private readonly ByteBuf _emptyBuf;

        // heap buffers by default
        protected AbstractByteBufAllocator()
            : this(false)
        { }

        protected AbstractByteBufAllocator(bool preferDirect)
        {
            _directByDefault = preferDirect; // TODO PlatformDependent needed??
            _emptyBuf = new EmptyByteBuf(); // alloc not used
        }

        public ByteBuf Buffer(int initialCapacity, int maxCapacity)
        {
            if (_directByDefault)
            {
                return DirectBuffer(initialCapacity, maxCapacity);
            }
            return HeapBuffer(initialCapacity, maxCapacity);
        }

        public ByteBuf DirectBuffer(int initialCapacity)
        {
            return DirectBuffer(initialCapacity, Int32.MaxValue);
        }

        public ByteBuf DirectBuffer(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity == 0 && maxCapacity == 0)
            {
                return _emptyBuf;
            }
            Validate(initialCapacity, maxCapacity);
            return NewDirectBuffer(initialCapacity, maxCapacity);
        }

        public ByteBuf HeapBuffer(int initialCapacity)
        {
            return HeapBuffer(initialCapacity, Int32.MaxValue);
        }

        public ByteBuf HeapBuffer(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity == 0 && maxCapacity == 0)
            {
                return _emptyBuf;
            }
            Validate(initialCapacity, maxCapacity);
            return NewHeapBuffer(initialCapacity, maxCapacity);
        }

        protected abstract ByteBuf NewDirectBuffer(int initialCapacity, int maxCapacity);

        protected abstract ByteBuf NewHeapBuffer(int initialCapacity, int maxCapacity);

        private static void Validate(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentException("initialCapacity: " + initialCapacity + " (expectd: 0+)");
            }
            if (initialCapacity > maxCapacity)
            {
                throw new ArgumentException(String.Format(
                        "initialCapacity: {0} (expected: not greater than maxCapacity({1})",
                        initialCapacity, maxCapacity));
            }
        }
    }
}
