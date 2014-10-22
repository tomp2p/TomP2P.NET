using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Message
{
    public sealed class MessageHeaderCodec
    {
        public const int HeaderSize = 58; // bytes
        
        public static void EncodeHeader(MemoryStream buffer, Message message)
        {
            
        }

        public static Message DecodeHeader(MemoryStream buffer, IPEndPoint recipient, IPEndPoint sender)
        {
            throw new NotImplementedException();
        }
    }
}
