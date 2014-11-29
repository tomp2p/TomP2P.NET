using NLog;
using System.Net;
using TomP2P.Extensions.Workaround;
using TomP2P.Peers;

namespace TomP2P.Message
{
    /// <summary>
    /// Encodes and decodes the header of a <see cref="Message"/>.
    /// </summary>
    public static class MessageHeaderCodec
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public const int HeaderSize = 58; // bytes
        
        static MessageHeaderCodec()
        { }

        /// <summary>
        /// Encodes a message object.
        /// The format looks as follows: 28 bit P2P version, 4 bit message type, 32 bit message ID, 8 bit message command,
        /// 160 bit senderSocket ID, 16 bit senderSocket TCP port, 16 bit senderSocket UDP port, 160 bit recipientSocket ID, 32 bit content types, 8 bit options.
        /// In total, the header is of size 58 bytes.
        /// </summary>
        /// <param name="buffer">The buffer to encode to.</param>
        /// <param name="message">The message with the header that will be encoded.</param>
        public static void EncodeHeader(JavaBinaryWriter buffer, Message message)
        {
            // TODO add log statemet, also in Java version
            int versionAndType = message.Version << 4 | ((int)message.Type & Utils.Utils.Mask0F); // TODO check if ordinal works

            buffer.WriteInt(versionAndType); // 4
            buffer.WriteInt(message.MessageId); // 8
            buffer.WriteByte(message.Command); // 9
            buffer.WriteBytes(message.Sender.PeerId.ToByteArray()); // 29
            buffer.WriteShort((short) message.Sender.TcpPort); // 31 
            buffer.WriteShort((short) message.Sender.UdpPort); // 33
            buffer.WriteBytes(message.Recipient.PeerId.ToByteArray()); // 53
            buffer.WriteInt(EncodeContentTypes(message.ContentTypes)); // 57
            buffer.WriteByte((sbyte) (message.Sender.Options << 4 | message.Options)); // 58 // TODO check if works
        }

        /// <summary>
        /// Decodes a message object.
        /// The format looks as follows: 28 bit P2P version, 4 bit message type, 32 bit message ID, 8 bit message command,
        /// 160 bit senderSocket ID, 16 bit senderSocket TCP port, 16 bit senderSocket UDP port, 160 bit recipientSocket ID, 32 bit content types, 8 bit options.
        /// In total, the header is of size 58 bytes.
        /// </summary>
        /// <param name="buffer">The buffer to decode from.</param>
        /// <param name="recipientSocket">The recipientSocket of the message.</param>
        /// <param name="senderSocket">The senderSocket of the packet, which has been set in the socket class.</param> // TODO check if true
        /// <returns>The partial message where only the header fields are set.</returns>
        public static Message DecodeHeader(JavaBinaryReader buffer, IPEndPoint recipientSocket, IPEndPoint senderSocket)
        {
            Logger.Debug("Decode message. Recipient: {0}, Sender: {1}", recipientSocket, senderSocket);

            var message = new Message();

            int versionAndType = buffer.ReadInt(); // 4
            message.SetVersion(versionAndType >> 4);
            message.SetType((Message.MessageType)(versionAndType & Utils.Utils.Mask0F)); // TODO does this work? (2x)
            message.SetMessageId(buffer.ReadInt()); // 8
            message.SetCommand(buffer.ReadByte()); // 9 // TODO check conversion with Java version
            var senderId = ReadId(buffer); // 29
            int tcpPort = buffer.ReadUShort(); // 31 // TODO check if should be read as short (same as encode)
            int udpPort = buffer.ReadUShort(); // 33
            var recipientId = ReadId(buffer); // 53
            int contentTypes = buffer.ReadInt(); // 57
            int options = buffer.ReadUByte(); // 58 // TODO check if should be read as unsigned/signed

            message.SetRecipient(new PeerAddress(recipientId, recipientSocket));
            message.HasContent(contentTypes != 0);
            message.SetContentType(DecodeContentTypes(contentTypes, message));
            message.SetOptions(options & Utils.Utils.Mask0F);

            // set the address as we see it, important for port forwarding identification
            int senderOptions = options >> 4;
            var pa = new PeerAddress(senderId, senderSocket.Address, tcpPort, udpPort, senderOptions);

            message.SetSender(pa);
            message.SetSenderSocket(senderSocket);
            message.SetRecipientSocket(recipientSocket);

            return message;
        }

        /// <summary>
        /// Encodes the 8 content types to an integer (32 bit).
        /// </summary>
        /// <param name="contentTypes">The 8 content types to be encoded.</param>
        /// <returns>The encoded 32 bit integer.</returns>
        public static int EncodeContentTypes(Message.Content[] contentTypes)
        {
            int result = 0;
            for (int i = 0; i < Message.ContentTypeLength/2; i++)
            {
                if (contentTypes[i*2] != Message.Content.Empty) // TODO check port
                {
                    result |= ((int) contentTypes[i*2] << (i*8)); // TODO check ordinal
                }
                if (contentTypes[i*2 + 1] != Message.Content.Empty)
                {
                    result |= ((int) contentTypes[i*2 + 1] << 4) << (i*8);
                }
            }
            return result;
        }

        /// <summary>
        /// Decodes the 8 content types from an integer (32 bit).
        /// </summary>
        /// <param name="contentTypes">The 8 content types to be decoded.</param>
        /// <param name="message">The decoded content types.</param>
        /// <returns></returns>
        public static Message.Content[] DecodeContentTypes(int contentTypes, Message message)
        {
            var result = new Message.Content[Message.ContentTypeLength];
            for (int i = 0; i < Message.ContentTypeLength; i++)
            {
                var content = (Message.Content) (contentTypes & Utils.Utils.Mask0F);
                result[i] = content;
                if (content == Message.Content.PublicKeySignature)
                {
                    message.SetHintSign();
                }
                contentTypes >>= 4;
            }
            return result;
        }

        /// <summary>
        /// Reads a <see cref="Number160"/> from a buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static Number160 ReadId(JavaBinaryReader buffer)
        {
            var me = new sbyte[Number160.ByteArraySize];
            buffer.ReadBytes(me);
            return new Number160(me);
        }
    }
}
