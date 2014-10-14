using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TomP2P.Tests.Message
{
    [TestFixture]
    public class MessageTests
    {
        [Test]
        public void SetContentTypeTest()
        {
            var message = new TomP2P.Message.Message();

            message.SetContentType(TomP2P.Message.Message.Content.Empty);
        }
    }
}
