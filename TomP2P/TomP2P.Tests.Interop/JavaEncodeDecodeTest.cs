using System;
using NUnit.Framework;
using System.IO;
using TomP2P.Extensions.Netty;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Tests.Interop
{
    /// <summary>
    /// These tests check the binary encoding/decoding of data types between Java and .NET.
    /// </summary>
    [TestFixture]
    public class JavaEncodeDecodeTest
    {
        [Test]
        public void TestEncodeByte()
        {
            var buffer = AlternativeCompositeByteBuf.CompBuffer();

            // Java byte is signed
            for (int i = sbyte.MinValue; i <= sbyte.MaxValue; i++) // -128 ... 127
            {
                buffer.WriteByte((sbyte) i);
            }

            var bytes = InteropUtil.ExtractBytes(buffer);

            bool interopResult = JarRunner.WriteBytesAndTestInterop(bytes);
            Assert.IsTrue(interopResult);
        }

        [Test]
        public void TestEncodeBytes()
        {
            AlternativeCompositeByteBuf buffer = AlternativeCompositeByteBuf.CompBuffer();

            // Java byte is signed
            sbyte[] byteArray = new sbyte[256];
            for (int i = 0, b = sbyte.MinValue; b <= sbyte.MaxValue; i++, b++) // -128 ... 127
            {
                byteArray[i] = (sbyte) b;
            }

            buffer.WriteBytes(byteArray);

            var bytes = InteropUtil.ExtractBytes(buffer);

            bool interopResult = JarRunner.WriteBytesAndTestInterop(bytes);
            Assert.IsTrue(interopResult);
        }

        [Test]
        public void TestEncodeShort()
        {
            AlternativeCompositeByteBuf buffer = AlternativeCompositeByteBuf.CompBuffer();

            buffer.WriteShort(short.MinValue); // -32,768
            buffer.WriteShort(-256);
            buffer.WriteShort(-255);
            buffer.WriteShort(-128);
            buffer.WriteShort(-127);
            buffer.WriteShort(-1);
            buffer.WriteShort(0);
            buffer.WriteShort(1);
            buffer.WriteShort(127);
            buffer.WriteShort(128);
            buffer.WriteShort(255);
            buffer.WriteShort(256);
            buffer.WriteShort(short.MaxValue);  // 32,767

            var bytes = InteropUtil.ExtractBytes(buffer);

            bool interopResult = JarRunner.WriteBytesAndTestInterop(bytes);
            Assert.IsTrue(interopResult);
        }

        [Test]
        public void TestEncodeInt()
        {
            AlternativeCompositeByteBuf buffer = AlternativeCompositeByteBuf.CompBuffer();

            buffer.WriteInt(int.MinValue);  //-2147483648
            buffer.WriteInt(-256);
            buffer.WriteInt(-255);
            buffer.WriteInt(-128);
            buffer.WriteInt(-127);
            buffer.WriteInt(-1);
            buffer.WriteInt(0);
            buffer.WriteInt(1);
            buffer.WriteInt(127);
            buffer.WriteInt(128);
            buffer.WriteInt(255);
            buffer.WriteInt(256);
            buffer.WriteInt(int.MaxValue);  // 2147483647

            var bytes = InteropUtil.ExtractBytes(buffer);

            bool interopResult = JarRunner.WriteBytesAndTestInterop(bytes);
            Assert.IsTrue(interopResult);
        }

        [Test]
        public void TestEncodeLong()
        {
            AlternativeCompositeByteBuf buffer = AlternativeCompositeByteBuf.CompBuffer();

            buffer.WriteLong(long.MinValue);  //-923372036854775808
            buffer.WriteLong(-256);
            buffer.WriteLong(-255);
            buffer.WriteLong(-128);
            buffer.WriteLong(-127);
            buffer.WriteLong(-1);
            buffer.WriteLong(0);
            buffer.WriteLong(1);
            buffer.WriteLong(127);
            buffer.WriteLong(128);
            buffer.WriteLong(255);
            buffer.WriteLong(256);
            buffer.WriteLong(long.MaxValue);  // 923372036854775807

            var bytes = InteropUtil.ExtractBytes(buffer);

            bool interopResult = JarRunner.WriteBytesAndTestInterop(bytes);
            Assert.IsTrue(interopResult);
        }

        [Test]
        public void TestDecodeByte()
        {
            var bytes = JarRunner.RequestJavaBytes();

            var ms = new MemoryStream(bytes);
            var br = new JavaBinaryReader(ms);

            // Java byte is signed
            for (int i = sbyte.MinValue; i <= sbyte.MaxValue; i++) // -128 ... 127
            {
                sbyte b = br.ReadByte();
                Assert.IsTrue(i == b);
            }
        }

        [Test]
        public void TestDecodeBytes()
        {
            var bytes = JarRunner.RequestJavaBytes();

            var ms = new MemoryStream(bytes);
            var br = new JavaBinaryReader(ms);

            // Java byte is signed
            var byteArray = new sbyte[256];
            br.ReadBytes(byteArray);

            for (int i = 0, b = sbyte.MinValue; i <= sbyte.MaxValue; i++, b++) // -128 ... 127
            {
                Assert.IsTrue(b == byteArray[i]);
            }
        }

        [Test]
        public void TestDecodeInt()
        {
            var bytes = JarRunner.RequestJavaBytes();

            var ms = new MemoryStream(bytes);
            var br = new JavaBinaryReader(ms);

            int val1 = br.ReadInt();
            int val2 = br.ReadInt();
            int val3 = br.ReadInt();
            int val4 = br.ReadInt();
            int val5 = br.ReadInt();
            int val6 = br.ReadInt();
            int val7 = br.ReadInt();
            int val8 = br.ReadInt();
            int val9 = br.ReadInt();
            int val10 = br.ReadInt();
            int val11 = br.ReadInt();
            int val12 = br.ReadInt();
            int val13 = br.ReadInt();

            Assert.IsTrue(val1 == int.MinValue);
            Assert.IsTrue(val2 == -256);
            Assert.IsTrue(val3 == -255);
            Assert.IsTrue(val4 == -128);
            Assert.IsTrue(val5 == -127);
            Assert.IsTrue(val6 == -1);
            Assert.IsTrue(val7 == 0);
            Assert.IsTrue(val8 == 1);
            Assert.IsTrue(val9 == 127);
            Assert.IsTrue(val10 == 128);
            Assert.IsTrue(val11 == 255);
            Assert.IsTrue(val12 == 256);
            Assert.IsTrue(val13 == int.MaxValue);
        }

        [Test]
        public void TestDecodeLong()
        {
            var bytes = JarRunner.RequestJavaBytes();

            var ms = new MemoryStream(bytes);
            var br = new JavaBinaryReader(ms);

            long val1 = br.ReadLong();
            long val2 = br.ReadLong();
            long val3 = br.ReadLong();
            long val4 = br.ReadLong();
            long val5 = br.ReadLong();
            long val6 = br.ReadLong();
            long val7 = br.ReadLong();
            long val8 = br.ReadLong();
            long val9 = br.ReadLong();
            long val10 = br.ReadLong();
            long val11 = br.ReadLong();
            long val12 = br.ReadLong();
            long val13 = br.ReadLong();

            Assert.IsTrue(val1 == long.MinValue);
            Assert.IsTrue(val2 == -256);
            Assert.IsTrue(val3 == -255);
            Assert.IsTrue(val4 == -128);
            Assert.IsTrue(val5 == -127);
            Assert.IsTrue(val6 == -1);
            Assert.IsTrue(val7 == 0);
            Assert.IsTrue(val8 == 1);
            Assert.IsTrue(val9 == 127);
            Assert.IsTrue(val10 == 128);
            Assert.IsTrue(val11 == 255);
            Assert.IsTrue(val12 == 256);
            Assert.IsTrue(val13 == long.MaxValue);
        }
    }
}
