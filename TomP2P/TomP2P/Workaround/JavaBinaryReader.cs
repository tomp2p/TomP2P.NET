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
        /// Reads a 4-byte integer from the current stream and advances the current position of the stream by 4 bytes.
        /// </summary>
        /// <returns></returns>
        public int ReadInt()
        {
            // NOTE: _br.ReadInt32() would read in little-endian fashion
            
            // read bytes in big-endian fashion
            byte b1 = _br.ReadByte();
            byte b2 = _br.ReadByte();
            byte b3 = _br.ReadByte();
            byte b4 = _br.ReadByte();

            // shift bytes to their position and sum up their int values
            return ((b1 << 24) + (b2 << 16) + (b3 << 8) + b4);
        }

        public long ReadLong()
        {
            /* WORKS !!
            var v1 = (long)_br.ReadByte() & Mask0XFf;
            var v2 = (long)_br.ReadByte() & Mask0XFf;
            var v3 = (long)_br.ReadByte() & Mask0XFf;
            var v4 = (long)_br.ReadByte() & Mask0XFf;
            var v5 = (long)_br.ReadByte() & Mask0XFf;
            var v6 = (long)_br.ReadByte() & Mask0XFf;
            var v7 = (long)_br.ReadByte() & Mask0XFf;
            var v8 = (long)_br.ReadByte() & Mask0XFf;*/

            // alternative: _br.ReadByte & (long)Mask0XFf
            // alternative: (long) _br.ReadByte() & Mask0XFf

            var v1 = (long)_br.ReadByte();
            var v2 = (long)_br.ReadByte();
            var v3 = (long)_br.ReadByte();
            var v4 = (long)_br.ReadByte();
            var v5 = (long)_br.ReadByte();
            var v6 = (long)_br.ReadByte();
            var v7 = (long)_br.ReadByte();
            var v8 = (long)_br.ReadByte();

            // big-endian (java) -> little-endian (.NET)
            return ((v1 << 56) + (v2 << 48) + (v3 << 40) + (v4 << 32)
                + (v5 << 24) + (v6 << 16) + (v7 << 8) + v8);
        }
    }
}
