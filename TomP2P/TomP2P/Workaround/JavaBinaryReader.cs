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

        private const int Mask0XFf = 0xFF;  // 1111 1111

        public JavaBinaryReader(Stream input)
        {
            _br = new BinaryReader(input);
        }

        /// <summary>
        /// Reads a 4-byte integer from the current stream and advances the current position of the stream by 4 bytes.
        /// </summary>
        /// <returns></returns>
        public int ReadInt32()
        {
            // read 4 bytes (32 bit)
            // apply 0xFF mask to convert from Java's signed to unsigned

            var v1 = _br.ReadByte() & Mask0XFf;
            var v2 = _br.ReadByte() & Mask0XFf;
            var v3 = _br.ReadByte() & Mask0XFf;
            var v4 = _br.ReadByte() & Mask0XFf;

            // left-shift the bytes to their according place
            return ((v1 << 24) + (v2 << 16) + (v3 << 8) + v4);
        }
    }
}
