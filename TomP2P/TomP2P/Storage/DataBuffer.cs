using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TomP2P.Extensions;

namespace TomP2P.Storage
{
    public class DataBuffer : IEquatable<DataBuffer>
    {
        private readonly IList<ByteBuf> _buffers;

        public long AlreadyTransferred { private set; get; }

        public DataBuffer()
            : this(1)
        { }

        public DataBuffer(int nrOfBuffers)
        {
            _buffers = new List<ByteBuf>(nrOfBuffers);
        }

        public DataBuffer(sbyte[] buffer, int offset, int length)
        {
            _buffers = new List<ByteBuf>(1);
            var buf = Unpooled.WrappedBuffer(buffer, offset, length);
            _buffers.Add(buf);
        }

        /// <summary>
        /// Creates a DataBuffer and adds the MemoryStream to it.
        /// </summary>
        /// <param name="buf"></param>
        public DataBuffer(ByteBuf buf)
        {
            _buffers = new List<ByteBuf>(1);
            _buffers.Add(buf.Slice());
            // TODO retain needed?
        }

        public DataBuffer(IList<ByteBuf> buffers)
        {
            _buffers = new List<ByteBuf>(buffers.Count);
            foreach (var buf in _buffers)
            {
                _buffers.Add(buf.Duplicate());
                // TODO retain needed?
            }
        }

        ~DataBuffer()
        {
            // TODO release needed?
        }

        public DataBuffer Add(DataBuffer dataBuffer)
        {
            lock (_buffers)
            {
                foreach (var buf in dataBuffer._buffers)
                {
                    _buffers.Add(buf.Duplicate());
                    // TODO retain needed?
                }
            }
            return this;
        }

        /// <summary>
        /// From here, work with shallow copies.
        /// </summary>
        /// <returns>Shallow copy of this DataBuffer.</returns>
        public DataBuffer ShallowCopy()
        {
            DataBuffer db;
            lock (_buffers)
            {
                db = new DataBuffer(_buffers);
            }
            return db;
        }

        /// <summary>
        /// Gets the backing list of MemoryStreams.
        /// </summary>
        /// <returns>The backing list of MemoryStreams.</returns>
        public IList<MemoryStream> BufferList()
        {
            DataBuffer copy = ShallowCopy();
            IList<MemoryStream> buffers = new List<MemoryStream>(copy._buffers.Count);
            foreach (var buf in copy._buffers)
            {
                foreach (var bb in buf.NioBuffers())
                {
                    buffers.Add(bb);
                }
            }
            return buffers;
        }

        // TODO merge the two/four, needs implementation of .NET "ByteBuf" with reader and writer
        public ByteBuf ToByteBuf()
        {
            // TODO check if works
            DataBuffer copy = ShallowCopy();
            return Unpooled.WrappedBuffer(copy._buffers.ToArray());
        }

        public ByteBuf[] ToByteBufs()
        {
            // TODO check if works
            DataBuffer copy = ShallowCopy();
            return copy._buffers.ToArray();
        }

        public MemoryStream[] ToByteBuffer() // TODO use possible ByteBuffer wrapper
        {
            return ToByteBuf().NioBuffers();
        }

        public void TransferTo(AlternativeCompositeByteBuf buf)
        {
            // TODO check if works
            DataBuffer copy = ShallowCopy();
            foreach (var buffer in copy._buffers)
            {
                buf.AddComponent(buffer);
                AlreadyTransferred += buffer.ReadableBytes;
            }
        }

        public int TransferFrom(AlternativeCompositeByteBuf buf, int remaining)
        {
            // TODO check if works
            var readable = buf.ReadableBytes;
            var index = buf.ReaderIndex;
            var length = Math.Min(remaining, readable);

            if (length == 0)
            {
                return 0;
            }

            IList<ByteBuf> decoms = buf.Decompose(index, length);
            foreach (var decom in decoms)
            {
                lock (_buffers)
                {
                    // this is already a slice
                    _buffers.Add(decom);
                }
            }

            AlreadyTransferred += Length;
            buf.SetReaderIndex(buf.ReaderIndex + length);
            return length;
        }

        public void ResetAlreadyTransferred()
        {
            AlreadyTransferred = 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as DataBuffer);
        }

        public bool Equals(DataBuffer other)
        {
            return other.ToByteBuffer().Equals(ToByteBuffer());
        }

        public override int GetHashCode()
        {
            return ToByteBuffer().GetHashCode();
        }

        public int Length
        {
            get
            {
                int length = 0;
                DataBuffer copy = ShallowCopy();
                foreach (var buffer in copy._buffers)
                {
                    length += buffer.WriterIndex;
                }
                return length;
            }
        }

        public byte[] Bytes
        {
            get
            {
                var bufs = ToByteBuffer();
                int bufsLength = bufs.Length;
                long size = 0;
                for (int i = 0; i < bufsLength; i++)
                {
                    size += bufs[i].Remaining();
                }

                byte[] retVal = new byte[size];
                long offset = 0;
                for (int i = 0; i < bufsLength; i++)
                {
                    long remaining = bufs[i].Remaining();
                    bufs[i].Get(retVal, offset, remaining);
                    offset += remaining;
                }
                return retVal;
            }
        }
    }
}
