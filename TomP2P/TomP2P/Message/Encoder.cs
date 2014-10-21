using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection;
using TomP2P.Storage;

namespace TomP2P.Message
{
    public class Encoder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private bool _header = false;
        private bool _resume = false;
        private readonly ISignatureFactory _signatureFactory;
        
        public Message Message { get; private set; }

        public Encoder(ISignatureFactory signatureFactory)
        {
            _signatureFactory = signatureFactory;
        }

        // TODO throw exceptions?
        public bool Write(MemoryStream buffer, Message message, ISignatureCodec signatureCodec)
        {
            Message = message;
            Logger.Debug("Message for outbound {0}.", message);

            if (!_header)
            {
                // TODO check if buffer is really changed (pass-by-reference)
                MessageHeaderCodec.EncodeHeader(buffer, message);
                _header = true;
            }
            else
            {
                Logger.Debug("Send a follow-up message {0}.", message);
                _resume = true;
            }

            bool done = Loop(buffer);
            Logger.Debug("Message encoded {0}.", message);

            // write out what we have
            // TODO chech isReadable() equivalent
            if (buffer.CanRead && done)
            {
                // check if message needs to be signed
                if (message.IsSign)
                {
                    // we sign if we did not provide a signature already
                    if (signatureCodec == null)
                    {
                        signatureCodec = _signatureFactory.Sign(message.PrivateKey, buffer);
                    }
                    // in case of relay, we have a signature, so we need to resuse this
                    signatureCodec.Write(buffer);
                }
            }

            return done;
        }

        public void Reset()
        {
            _header = false;
            _resume = false;
        }

        // TODO throw exception?
        private bool Loop(MemoryStream buffer)
        {
            MessageContentIndex next;

            // TODO check if queue returns null if empty
            while ((next = Message.ContentReferences.Peek()) != null)
            {
                // TODO check buffer equivalent
                long start = buffer.Position;
                Message.Content content = next.Content;

                // TODO make all writes async
                // TODO use BinaryWriter
                // TODO what happens if null is serialized? exception?
                byte[] bytes;
                byte[] lengthBytes;
                switch (content)
                {
                    case Message.Content.Key:
                        bytes = Message.Key(next.Index).ToByteArray();
                        buffer.Write(bytes, 0, bytes.Length);
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.Integer:
                        bytes = BitConverter.GetBytes(Message.IntAt(next.Index));
                        buffer.Write(bytes, 0, bytes.Length);
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.Long:
                        bytes = BitConverter.GetBytes(Message.LongAt(next.Index));
                        buffer.Write(bytes, 0, bytes.Length);
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.SetNeighbors:
                        var neighborSet = Message.NeighborsSet(next.Index);
                        // write length
                        lengthBytes = BitConverter.GetBytes(neighborSet.Size);
                        buffer.Write(lengthBytes, 0, lengthBytes.Length);
                        foreach (var neighbor in neighborSet.Neighbors)
                        {
                            bytes = neighbor.ToByteArray();
                            buffer.Write(bytes, 0, bytes.Length);
                        }
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.SetPeerSocket:
                        var list = Message.PeerSocketAddresses;
                        // write length
                        lengthBytes = BitConverter.GetBytes(list.Count);
                        buffer.Write(lengthBytes, 0, lengthBytes.Length);
                        foreach (var psa in list)
                        {
                            // write IP version flag
                            buffer.WriteByte(psa.IsIPv4 ? (byte)0 : (byte)1); // TODO does this work?
                            bytes = psa.ToByteArray();
                            buffer.Write(bytes, 0, bytes.Length);
                        }
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.BloomFilter:
                        var bf = Message.BloomFilter(next.Index);
                        bf.ToByteBuffer(buffer); // TODO make better, don't write to buffer in encapsulated style?
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.SetKey640:
                        var keys = Message.KeyCollection(next.Index);
                        // write length
                        lengthBytes = BitConverter.GetBytes(keys.Size);
                        buffer.Write(lengthBytes, 0, lengthBytes.Length);
                        if (keys.IsConvert)
                        {
                            foreach (var key in keys.KeysConvert)
                            {
                                
                            }
                        }
                        else
                        {
                            
                        }
                        Message.ContentReferences.Dequeue();
                        break;
                }

                Logger.Debug("Wrote in encoder for {0} {1}.", content, buffer.Position - start);
            }
            return true;
        }

        // TODO throw exceptions?
        // TODO return type long instead of int?
        private long EncodeData(MemoryStream buffer, Data data, bool isConvertMeta, bool isReply)
        {
            data = isConvertMeta ? data.DuplicateMeta() : data.Duplicate();

            if (isReply)
            {
                var ttl = (int) ((data.ExpirationMillis - Utils.Utils.GetCurrentMillis())/1000);
                data.SetTtlSeconds(ttl < 0 ? 0 : ttl);
            }

            // TODO check again, port isn't easy
            long startWriter = buffer.Position;
            data.EncodeHeader(buffer, _signatureFactory);
            data.EncodeBuffer(buffer);
            data.EncodeDone(buffer, _signatureFactory, Message.PrivateKey);

            return buffer.Position - startWriter;
        }
    }
}
