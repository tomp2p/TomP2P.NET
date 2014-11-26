using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Storage
{
    public class DataBuffer : IEquatable<DataBuffer>
    {
        private readonly IList<MemoryStream> _buffers; // TODO should be an AlternativeCompositeByteBuf

        public long AlreadyTransferred { private set; get; }

        public DataBuffer()
            : this(1)
        { }

        public DataBuffer(int nrOfBuffers)
        {
            _buffers = new List<MemoryStream>(nrOfBuffers);
        }

        public DataBuffer(sbyte[] buffer, int offset, int length)
        {
            _buffers = new List<MemoryStream>(1);

            // TODO check, port is not trivial
            var buf = new MemoryStream(buffer.ToByteArray(), offset, length);
            _buffers.Add(buf);
        }

        /// <summary>
        /// Creates a DataBuffer and adds the MemoryStream to it.
        /// </summary>
        /// <param name="buf"></param>
        public DataBuffer(MemoryStream buf)
        {
            // TODO check, port is not trivial
            _buffers = new List<MemoryStream>(1);
            _buffers.Add(buf.Slice());
            // TODO retain needed?
        }

        public DataBuffer(IList<MemoryStream> buffers)
        {
            _buffers = new List<MemoryStream>(_buffers.Count);
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
                // TODO check if works, port not trivial
                buffers.Add(buf);
            }
            return buffers;
        }

        // TODO merge the two/four, needs implementation of .NET "ByteBuf" with reader and writer
        public JavaBinaryWriter ToJavaBinaryWriter()
        {
            DataBuffer copy = ShallowCopy();
            MemoryStream[] buffers = copy._buffers.ToArray();

            // TODO this most probably doesn't work
            for (int i = 1; i < buffers.Length; i++)
            {
                buffers[i].CopyTo(buffers[0]);
            }
            return new JavaBinaryWriter(buffers[0]);
        }

        public JavaBinaryReader ToJavaBinaryReader()
        {
            DataBuffer copy = ShallowCopy();
            MemoryStream[] buffers = copy._buffers.ToArray();

            // TODO this most probably doesn't work
            for (int i = 1; i < buffers.Length; i++)
            {
                buffers[i].CopyTo(buffers[0]);
            }
            return new JavaBinaryReader(buffers[0]);
        }

        public JavaBinaryWriter[] ToJavaBinaryWriters()
        {
            // TODO check if port is correct
            DataBuffer copy = ShallowCopy();
            var writers = new JavaBinaryWriter[copy._buffers.Count];
            for (int i = 0; i < copy._buffers.Count; i++)
            {
                writers[i] = new JavaBinaryWriter(copy._buffers[i]);
            }
            return writers;
        }

        public JavaBinaryReader[] ToJavaBinaryReaders()
        {
            // TODO check if port is correct
            DataBuffer copy = ShallowCopy();
            var readers = new JavaBinaryReader[copy._buffers.Count];
            for (int i = 0; i < copy._buffers.Count; i++)
            {
                readers[i] = new JavaBinaryReader(copy._buffers[i]);
            }
            return readers;
        }

        public MemoryStream[] ToByteBuffer()
        {
            DataBuffer copy = ShallowCopy();
            return copy._buffers.ToArray();
        }

        public void TransferTo(JavaBinaryWriter buffer)
        {
            // TODO implement
            throw new NotImplementedException();

            DataBuffer copy = ShallowCopy();
            foreach (var buf in copy._buffers)
            {
                // TODO buf.addComponent(buffer);
                AlreadyTransferred += buf.ReadableBytes();
            }
        }

        public int TransferFrom(JavaBinaryReader buffer, long remaining)
        {
            // TODO implement
            throw new NotImplementedException();

            var readable = buffer.ReadableBytes();
            var index = buffer.ReaderIndex();
            var length = Math.Min(remaining, readable);

            if (length == 0)
            {
                return 0;
            }
            // Java stuff

            AlreadyTransferred += Length;
            buffer.SetReaderIndex(buffer.ReaderIndex() + length);
            //return length;
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

        public long Length
        {
            get
            {
                long length = 0;
                DataBuffer copy = ShallowCopy();
                foreach (var buffer in copy._buffers)
                {
                    length += buffer.Position; // TODO check if correct, needs writerIndex
                }
                return length;
            }
        }


    }
}
