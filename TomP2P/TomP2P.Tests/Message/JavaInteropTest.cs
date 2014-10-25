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
            /*var m1 = Utils2.CreateDummyMessage();
            const int integer = 42;
            m1.SetIntValue(integer);*/

            //var encoder = new Encoder(null);
            var ms = new MemoryStream();
            var buffer = new JavaBinaryWriter(ms);

            //encoder.Write(buffer, m1, null);

            const int value = Int32.MaxValue; // 2147483647

            buffer.WriteInt32(value);

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
