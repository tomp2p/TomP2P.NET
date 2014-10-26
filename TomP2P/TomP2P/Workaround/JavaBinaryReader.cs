using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Workaround
{
    /// <summary>
    /// This class allows to read Java types that have been stored to a <code>byte[]</code>.
    /// Internally, a <see cref="BinaryReader"/> is used.
    /// </summary>
    public class JavaBinaryReader
    {
        private readonly BinaryReader _br;

        public JavaBinaryReader(Stream input)
        {
            _br = new BinaryReader(input);
        }

        /// <summary>
        /// Reads a 4-byte signed integer from the current stream and advances the current position of the stream by 4 bytes.
        /// </summary>
        /// <returns></returns>
        public int ReadInt()
        {
            // NOTE: _br.ReadInt32() would read in little-endian fashion (.NET)
            
            // read bytes in big-endian fashion (Java)
            byte b1 = _br.ReadByte();
            byte b2 = _br.ReadByte();
            byte b3 = _br.ReadByte();
            byte b4 = _br.ReadByte();

            // shift bytes to their position and sum up their int values
            return ((b1 << 24) + (b2 << 16) + (b3 << 8) + b4);
        }

        /// <summary>
        /// Reads a 8-byte signed integer from the current stream and advances the current position of the stream by 8 bytes.
        /// </summary>
        /// <returns></returns>
        public long ReadLong()
        {
            // NOTE: _br.ReadInt64() would read in little-endian fashion (.NET)

            // read bytes in big-endian fashion (Java)
            // direct implicit conversion to long, allows shifts > 24
            long v1 = _br.ReadByte();
            long v2 = _br.ReadByte();
            long v3 = _br.ReadByte();
            long v4 = _br.ReadByte();
            long v5 = _br.ReadByte();
            long v6 = _br.ReadByte();
            long v7 = _br.ReadByte();
            long v8 = _br.ReadByte();

            // shift bytes to their position and sum up their long values
            return ((v1 << 56) + (v2 << 48) + (v3 << 40) + (v4 << 32)
                + (v5 << 24) + (v6 << 16) + (v7 << 8) + v8);
        }

        /// <summary>
        /// Reads a signed byte from the current stream and advances the current position of the stream by 1 byte.
        /// </summary>
        /// <returns></returns>
        public sbyte ReadByte()
        {
            // Java byte is signed
            return _br.ReadSByte();
        }
    }
}
