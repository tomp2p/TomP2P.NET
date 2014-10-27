using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Workaround;

namespace TomP2P.Message
{
    /// <summary>
    /// Encodes and decodes the header of a <see cref="Message"/>.
    /// </summary>
    public sealed class MessageHeaderCodec
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public const int HeaderSize = 58; // bytes
        
        public static void EncodeHeader(JavaBinaryWriter buffer, Message message)
        {
            throw new NotImplementedException();
        }

        public static Message DecodeHeader(JavaBinaryReader buffer, IPEndPoint recipient, IPEndPoint sender)
        {
            throw new NotImplementedException();
        }
    }
}
