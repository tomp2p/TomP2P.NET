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
            
            // shift int bits to their position and convert to byte
            var b1 = (byte) (value >> 24);
            var b2 = (byte) (value >> 16);
            var b3 = (byte) (value >> 8);
            var b4 = (byte) (value);

            // write bytes in big-endian fashion (Java)
            _bw.Write(b1);
            _bw.Write(b2);
            _bw.Write(b3);
            _bw.Write(b4);
        }

        public void WriteLong(long value)
        {
            // signed -> unsigned
            // little-endian -> big-endian
            var b1 = (byte) ((ulong) value >> 56);
            var b2 = (byte) ((ulong) value >> 48);
            var b3 = (byte) ((ulong) value >> 40);
            var b4 = (byte) ((ulong) value >> 32);
            var b5 = (byte) ((ulong) value >> 24);
            var b6 = (byte) ((ulong) value >> 16);
            var b7 = (byte) ((ulong) value >> 8);
            var b8 = (byte) ((ulong) value);

            _bw.Write(b1);
            _bw.Write(b2);
            _bw.Write(b3);
            _bw.Write(b4);
            _bw.Write(b5);
            _bw.Write(b6);
            _bw.Write(b7);
            _bw.Write(b8);
        }

        public void WriteByte(byte value)
        {
            throw new NotImplementedException();
        }

        public void WriteBytes(byte[] value)
        {
            throw new NotImplementedException();
        }

        public bool CanRead
        {
            get { throw new NotImplementedException(); }
        }

        public int WriterIndex { get; private set; }
        public int ReaderIndex { get; private set; }
        public byte[] Buffer { get; private set; }
    }
}
