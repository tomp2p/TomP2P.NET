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

        private const int Mask0XFf = 0xFF;  // 00000000 00000000 00000000 11111111

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
            // read 4 bytes (32 bit)
            // apply 0xFF (32 bit) mask to convert from Java's signed int to .NET unsigned int

            var v1 = _br.ReadByte() & Mask0XFf;
            int v2 = _br.ReadByte() & Mask0XFf;
            int v3 = _br.ReadByte() & Mask0XFf;
            int v4 = _br.ReadByte() & Mask0XFf;

            // left-shift the ints to their according place
            return ((v1 << 24) + (v2 << 16) + (v3 << 8) + v4);
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
            var v8 = (long)_br.ReadByte() & Mask0XFf;

            return ((v1 << 56) + (v2 << 48) + (v3 << 40) + (v4 << 32)
                + (v5 << 24) + (v6 << 16) + (v7 << 8) + v8);*/

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

            return ((v1 << 56) + (v2 << 48) + (v3 << 40) + (v4 << 32)
                + (v5 << 24) + (v6 << 16) + (v7 << 8) + v8);
        }
    }
}
