using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.Workaround;
using Decoder = TomP2P.Message.Decoder;

namespace TomP2P.Tests.Interop
{
    [TestFixture]
    public class MessageEncodeDecodeTest
    {
        [Test]
        public void TestMessageDecodeInt()
        {
            // create same message object as in Java
            var m1 = Utils2.CreateDummyMessage();
		    m1.SetIntValue(Int32.MinValue);
		    m1.SetIntValue(-256);
		    m1.SetIntValue(-128);
		    m1.SetIntValue(-1);
		    m1.SetIntValue(0);
		    m1.SetIntValue(1);
		    m1.SetIntValue(128);
		    m1.SetIntValue(Int32.MaxValue);
            
            // read Java encoded bytes
            var bytes = JarRunner.RequestJavaBytes();
            var ms = new MemoryStream(bytes);
            var br = new JavaBinaryReader(ms);

            var decoder = new Decoder(null); // TODO signaturefactory?

            decoder.Decode(br, m1.Recipient.CreateSocketTcp(), m1.Sender.CreateSocketTcp()); // TODO recipient/sender used?

            // compare Java encoded and .NET decoded objects
            var m2 = decoder.Message;

            CompareMessages(m1, m2);
        }

        private static void CompareMessages(Message.Message m1, Message.Message m2)
        {
            Assert.IsTrue(Utils.Utils.IsSameSets(m1.IntList, m2.IntList));
        }
    }
}
