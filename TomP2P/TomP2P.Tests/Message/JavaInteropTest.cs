using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Workaround;

namespace TomP2P.Tests.Message
{
    /// <summary>
    /// These tests have to be done manually as they exceed the boundary of the .NET platform.
    /// </summary>
    [TestFixture]
    public class JavaInteropTest
    {
        //private const string From = "D:/Desktop/interop/bytes-JAVA-encoded.txt";
        //private const string To = "D:/Desktop/interop/bytes-NET-encoded.txt";
        private const string From = "C:/Users/Christian/Desktop/interop/bytes-JAVA-encoded.txt";
        private const string To = "C:/Users/Christian/Desktop/interop/bytes-NET-encoded.txt";

        [Test]
        public void TestEncodeInt()
        {
            var ms = new MemoryStream();
            var buffer = new JavaBinaryWriter(ms);

            buffer.WriteInt32(int.MinValue);    //-2147483648
            buffer.WriteInt32(0);
            buffer.WriteInt32(int.MaxValue);  // 2147483647

            byte[] bytes = ms.GetBuffer();

            File.WriteAllBytes(To, bytes);
        }

        [Test]
        public void TestDecodeInt()
        {
            var bytes = File.ReadAllBytes(From);

            var ms = new MemoryStream(bytes); // assumes unsigned-bytes

            var br = new JavaBinaryReader(ms);

            int minVal = br.ReadInt();
            int zero = br.ReadInt();
            int maxVal = br.ReadInt();

            Assert.IsTrue(minVal == int.MinValue);
            Assert.IsTrue(zero == 0);
            Assert.IsTrue(maxVal == int.MaxValue);
        }

        [Test]
        public void TestEncodeLong()
        {
            
        }

        [Test]
        public void TestDecodeLong()
        {
            var bytes = File.ReadAllBytes(From);

            var ms = new MemoryStream(bytes);

            var br = new JavaBinaryReader(ms);

            long minVal = br.ReadLong();
            long zero = br.ReadLong();
            long maxVal = br.ReadLong();

            Assert.IsTrue(minVal == long.MinValue);
            Assert.IsTrue(zero == 0);
            Assert.IsTrue(maxVal == long.MaxValue);
        }

        [Test]
        public void TestEncodeByte()
        {
            
        }

        [Test]
        public void TestDecodeByte()
        {
            
        }

        [Test]
        public void TestEncodeBytes()
        {
            
        }

        [Test]
        public void TestDecodeBytes()
        {
            
        }
    }
}
