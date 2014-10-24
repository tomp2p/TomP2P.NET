using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Workaround;
using Encoder = TomP2P.Message.Encoder;
using Message = TomP2P.Message.Message;

namespace TomP2P.Tests.Message
{
    /// <summary>
    /// These tests have to be done manually as they exceed the boundary of the .NET platform.
    /// </summary>
    [TestFixture]
    public class JavaInteropTest
    {
        private const string From = "D:/Desktop/interop/bytes-JAVA-encoded.txt";
        private const string To = "D:/Desktop/interop/bytes-NET-encoded.txt";

        [Test]
        public void TestEncode()
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
        public void TestDecode()
        {
            var bytes = File.ReadAllBytes(From);

            var ms = new MemoryStream(bytes); // assumes unsigned-bytes

            var br = new JavaBinaryReader(ms);

            int value = br.ReadInt32();

        }
    }
}
