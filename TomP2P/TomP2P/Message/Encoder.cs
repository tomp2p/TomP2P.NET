﻿using System;
using NLog;
using TomP2P.Connection;
using TomP2P.Storage;
using TomP2P.Workaround;

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
        public bool Write(JavaBinaryWriter buffer, Message message, ISignatureCodec signatureCodec)
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
        private bool Loop(JavaBinaryWriter buffer)
        {
            MessageContentIndex next;

            // TODO check if queue returns null if empty
            while ((next = Message.ContentReferences.Peek()) != null)
            {
                // TODO check buffer equivalent
                long start = buffer.WriterIndex;
                Message.Content content = next.Content;

                // TODO make all writes async, also reads in decoder
                // TODO use BinaryWriter, also in decoder
                // TODO what happens if null is serialized? exception?
                switch (content)
                {
                    case Message.Content.Key:
                        buffer.WriteBytes(Message.Key(next.Index).ToByteArray());
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.Integer:
                        buffer.WriteInt32(Message.IntAt(next.Index));
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.Long:
                        buffer.WriteLong(Message.LongAt(next.Index));
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.SetNeighbors:
                        var neighborSet = Message.NeighborsSet(next.Index);
                        // write length
                        buffer.WriteByte((byte) neighborSet.Size); // TODO check if conversion is valid
                        foreach (var neighbor in neighborSet.Neighbors)
                        {
                            buffer.WriteBytes(neighbor.ToByteArray());
                        }
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.SetPeerSocket:
                        var list = Message.PeerSocketAddresses;
                        // write length
                        buffer.WriteByte((byte) list.Count); // TODO check if conversion is valid
                        foreach (var psa in list)
                        {
                            // write IP version flag
                            buffer.WriteByte(psa.IsIPv4 ? (byte)0 : (byte)1); // TODO check if conversion is valid
                            buffer.WriteBytes(psa.ToByteArray());
                        }
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.BloomFilter:
                        var bf = Message.BloomFilter(next.Index);
                        bf.ToByteBuffer(buffer);
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.SetKey640:
                        var keys = Message.KeyCollection(next.Index);
                        // write length
                        buffer.WriteInt32(keys.Size);
                        if (keys.IsConvert)
                        {
                            foreach (var key in keys.KeysConvert)
                            {
                                buffer.WriteBytes(keys.LocationKey.ToByteArray());
                                buffer.WriteBytes(keys.DomainKey.ToByteArray());
                                buffer.WriteBytes(key.ToByteArray());
                                buffer.WriteBytes(keys.VersionKey.ToByteArray());
                            }
                        }
                        else
                        {
                            foreach (var key in keys.Keys)
                            {
                                buffer.WriteBytes(key.LocationKey.ToByteArray());
                                buffer.WriteBytes(key.DomainKey.ToByteArray());
                                buffer.WriteBytes(key.ContentKey.ToByteArray());
                                buffer.WriteBytes(key.VersionKey.ToByteArray());
                            }
                        }
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.MapKey640Data:
                        var dm = Message.DataMap(next.Index);
                        // write length
                        buffer.WriteInt32(dm.Size);
                        if (dm.IsConvert)
                        {
                            foreach (var data in dm.DataMapConvert)
                            {
                                buffer.WriteBytes(dm.LocationKey.ToByteArray());
                                buffer.WriteBytes(dm.DomainKey.ToByteArray());
                                buffer.WriteBytes(data.Key.ToByteArray());
                                buffer.WriteBytes(dm.VersionKey.ToByteArray());

                                EncodeData(buffer, data.Value, dm.IsConvertMeta, !Message.IsRequest()); // TODO check reference passing
                            }
                        }
                        else
                        {
                            foreach (var data in dm.BackingDataMap)
                            {
                                buffer.WriteBytes(data.Key.LocationKey.ToByteArray());
                                buffer.WriteBytes(data.Key.DomainKey.ToByteArray());
                                buffer.WriteBytes(data.Key.ContentKey.ToByteArray());
                                buffer.WriteBytes(data.Key.VersionKey.ToByteArray());

                                EncodeData(buffer, data.Value, dm.IsConvertMeta, !Message.IsRequest()); // TODO check reference passing
                            }
                        }
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.MapKey640Keys:
                        var kmk = Message.KeyMap640Keys(next.Index);
                        // write length
                        buffer.WriteInt32(kmk.Size);
                        foreach (var data in kmk.KeysMap)
                        {
                            buffer.WriteBytes(data.Key.LocationKey.ToByteArray());
                            buffer.WriteBytes(data.Key.DomainKey.ToByteArray());
                            buffer.WriteBytes(data.Key.ContentKey.ToByteArray());
                            buffer.WriteBytes(data.Key.VersionKey.ToByteArray());

                            // write number of based-on keys
                            buffer.WriteByte((byte) data.Value.Count); // TODO check if conversion is valid

                            // write based-on keys
                            foreach (var key in data.Value)
                            {
                                buffer.WriteBytes(key.ToByteArray());
                            }
                        }
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.MapKey640Byte:
                        var kmb = Message.KeyMapByte(next.Index);
                        // write length
                        buffer.WriteInt32(kmb.Size);
                        foreach (var data in kmb.KeysMap)
                        {
                            buffer.WriteBytes(data.Key.LocationKey.ToByteArray());
                            buffer.WriteBytes(data.Key.DomainKey.ToByteArray());
                            buffer.WriteBytes(data.Key.ContentKey.ToByteArray());
                            buffer.WriteBytes(data.Key.VersionKey.ToByteArray());
                            
                            // write byte
                            buffer.WriteByte(data.Value);
                        }
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.ByteBuffer:
                        var b = Message.Buffer(next.Index);
                        if (!_resume)
                        {
                            buffer.WriteInt32(b.Length);
                        }
                        // write length
                        int readable = b.Readable;
                        buffer.WriteBytes(buffer.Buffer); // TODO check correctnes, port not trivial
                        if (b.IncRead(readable) == b.Length)
                        {
                            Message.ContentReferences.Dequeue();
                        }
                        else if (Message.IsStreaming())
                        {
                            Logger.Debug("Partial message of length {0} sent.", readable);
                            return false;
                        }
                        else
                        {
                            const string description = "Larger buffer has been announced, but not in message streaming mode. This is wrong.";
                            Logger.Error(description);
                            throw new SystemException(description);
                        }
                        break;
                    case Message.Content.SetTrackerData:
                        var td = Message.TrackerData(next.Index);
                        // write length
                        buffer.WriteByte((byte) td.PeerAddresses.Count); // TODO check if conversion is valid
                        foreach (var data in td.PeerAddresses)
                        {
                            buffer.WriteBytes(data.Key.PeerAddress.ToByteArray());

                            var d = data.Value.Duplicate();
                            EncodeData(buffer, d, false, !Message.IsRequest());
                        }
                        Message.ContentReferences.Dequeue();
                        break;
                    case Message.Content.PublicKeySignature:
                        // flag to encode public key
                        Message.SetHintSign();
                        // then, do the regular public key stuff -> no break
                        goto case Message.Content.PublicKey; // TODO check, else duplicate code
                    case Message.Content.PublicKey:
                        var pk = Message.PublicKey(next.Index);
                        _signatureFactory.EncodePublicKey(pk, buffer); // TODO check reference passing
                        Message.ContentReferences.Dequeue();
                        break;
                    default:
                        throw new SystemException("Unknown type: " + next.Content);
                }

                Logger.Debug("Wrote in encoder for {0} {1}.", content, buffer.WriterIndex - start);
            }
            return true;
        }

        // TODO throw exceptions?
        // TODO return type long instead of int?
        private long EncodeData(JavaBinaryWriter buffer, Data data, bool isConvertMeta, bool isReply)
        {
            data = isConvertMeta ? data.DuplicateMeta() : data.Duplicate();

            if (isReply)
            {
                var ttl = (int) ((data.ExpirationMillis - Utils.Utils.GetCurrentMillis())/1000);
                data.SetTtlSeconds(ttl < 0 ? 0 : ttl);
            }

            // TODO check again, port isn't easy
            long startWriter = buffer.WriterIndex;
            data.EncodeHeader(buffer, _signatureFactory);
            data.EncodeBuffer(buffer);
            data.EncodeDone(buffer, _signatureFactory, Message.PrivateKey);

            return buffer.WriterIndex - startWriter; // TODO remove?
        }
    }
}