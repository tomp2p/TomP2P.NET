using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
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

            var decoder = new Decoder(null); // TODO signaturefactory?
        }
    }
}
