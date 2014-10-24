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

        private const int Mask0XFf = 0xFF;  // 1111 1111

        public JavaBinaryWriter(Stream output)
        {
            _bw = new BinaryWriter(output);
        }

        /// <summary>
        /// Writes a 4-byte integer to the current stream and advances the stream position by 4 bytes.
        /// </summary>
        /// <param name="value"></param>
        public void WriteInt32(int value)
        {
            var b1 = (byte) (((uint) value >> 24) & Mask0XFf);
            var b2 = (byte) (((uint) value >> 16) & Mask0XFf);
            var b3 = (byte) (((uint) value >> 8) & Mask0XFf);
            var b4 = (byte) (((uint) value) & Mask0XFf);

            _bw.Write(b1);
            _bw.Write(b2);
            _bw.Write(b3);
            _bw.Write(b4);
        }

        public void WriteLong(long value)
        {
            throw new NotImplementedException();
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
