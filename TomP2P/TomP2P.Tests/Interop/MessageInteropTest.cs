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
    public class MessageInteropTest : InteropBaseTest
    {
        [Test]
        [Ignore]
        public void TestMessageEncode()
        {
            
        }

        [Test]
        [Ignore]
        public void TestMessageDecode()
        {
            var bytes = File.ReadAllBytes(From);
            var ms = new MemoryStream(bytes);
            var br = new JavaBinaryReader(ms);

            var decoder = new Decoder(null); // TODO signaturefactory?

            var m1 = Utils2.CreateDummyMessage();
            m1.SetIntValue(42);

            decoder.Decode(br, m1.Recipient.CreateSocketTcp(), m1.Sender.CreateSocketTcp()); // TODO recipient/sender used?


            var m2 = decoder.Message;

            CompareMessages(m1, m2);
        }

        private void CompareMessages(TomP2P.Message.Message m1, TomP2P.Message.Message m2)
        {
            Assert.IsTrue(Utils.Utils.IsSameSets(m1.IntList, m2.IntList));
        }
    }
}
