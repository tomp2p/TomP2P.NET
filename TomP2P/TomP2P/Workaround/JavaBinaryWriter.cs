using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Workaround
{
    /// <summary>
    /// This class allows to write Java types to a <code>byte[]</code>.
    /// Internally, a <see cref="BinaryWriter"/> is used.
    /// </summary>
    public class JavaBinaryWriter : IJavaBuffer
    {
        private readonly BinaryWriter _bw;

        public JavaBinaryWriter(Stream output)
        {
            _bw = new BinaryWriter(output);
        }

        /// <summary>
        /// Writes a 4-byte integer to the current stream and advances the stream position by 4 bytes.
        /// </summary>
        /// <param name="value"></param>
        public void WriteInt(int value)
        {
            // NOTE: _br.Write(int) would write in little-endian fashion (.NET)
            
            // shift int bits to their position and cast to byte
            var b1 = (byte) (value >> 24);
            var b2 = (byte) (value >> 16);
            var b3 = (byte) (value >> 8);
            var b4 = (byte) value;

            // write bytes in big-endian fashion (Java)
            _bw.Write(b1);
            _bw.Write(b2);
            _bw.Write(b3);
            _bw.Write(b4);
        }

        public void WriteLong(long value)
        {
            // NOTE: _br.Write(long) would write in little-endian fashion (.NET)

            // shift long bits to their position and cast to byte
            var b1 = (byte) (value >> 56);
            var b2 = (byte) (value >> 48);
            var b3 = (byte) (value >> 40);
            var b4 = (byte) (value >> 32);
            var b5 = (byte) (value >> 24);
            var b6 = (byte) (value >> 16);
            var b7 = (byte) (value >> 8);
            var b8 = (byte) value;

            // write bytes in big-endian fashion (Java)
            _bw.Write(b1);
            _bw.Write(b2);
            _bw.Write(b3);
            _bw.Write(b4);
            _bw.Write(b5);
            _bw.Write(b6);
            _bw.Write(b7);
            _bw.Write(b8);
        }

        public void WriteByte(sbyte value)
        {
            // Java byte is signed
            _bw.Write(value);
        }

        public void WriteBytes(sbyte[] src)
        {
            // Java byte is signed
            for (int i = 0; i < src.Length; i++)
            {
                WriteByte(src[i]);
            }
        }

        public bool CanRead
        {
            get { throw new NotImplementedException(); }
        }

        public int WriterIndex { get; private set; }
        public long ReaderIndex { get; private set; }
        public sbyte[] Buffer { get; private set; }
    }
}
