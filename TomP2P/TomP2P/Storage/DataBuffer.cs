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
    public class DataBuffer
    {
        private readonly IList<MemoryStream> _buffers;

        private int _alreadyTransferred = 0;

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
            }
        }

        public int AlreadyTransferred()
        {
            throw new NotImplementedException();
        }

        public int TransferFrom(JavaBinaryReader buffer, int remaining)
        {
            throw new NotImplementedException();
        }

        public void TransferTo(JavaBinaryWriter buffer)
        {
            throw new NotImplementedException();
        }

        // replaces toByteBuf
        public JavaBinaryWriter ToJavaBinaryWriter()
        {
            throw new NotImplementedException();
        }

        public JavaBinaryReader ToJavaBinaryReader()
        {
            throw new NotImplementedException();
        }

        public void ResetAlreadyTransferred()
        {
            throw new NotImplementedException();
        }
    }
}
