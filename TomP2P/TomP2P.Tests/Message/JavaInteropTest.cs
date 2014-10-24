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
            //var buffer = new MemoryStream();

            //encoder.Write(buffer, m1, null);

            //byte[] bytes = buffer.GetBuffer(); // gets unsigned bytes

            //byte[] bytes = new byte[] {0, 1, 2, 3};

        }

        [Test]
        public void TestDecode()
        {
            var bytes = File.ReadAllBytes(From);

            var ms = new MemoryStream(bytes); // assumes unsigned-bytes

            var br = new JavaBinaryReader(ms);



        }
    }
}
