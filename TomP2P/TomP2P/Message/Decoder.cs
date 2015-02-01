using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NLog;
using TomP2P.Connection;
using TomP2P.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty;
using TomP2P.Extensions.Workaround;
using TomP2P.P2P;
using TomP2P.Peers;
using TomP2P.Rpc;
using TomP2P.Storage;

namespace TomP2P.Message
{
    public class Decoder
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // TODO add attribute keys??

        private readonly Queue<Message.Content> _contentTypes = new Queue<Message.Content>();

        public Message Message { get; private set; }
        public Message.Content LastContent { get; private set; } // TODO check if correct

        private int _neighborSize = -1;
        private NeighborSet _neighborSet = null;

        private int _peerSocketAddressSize = -1;
        private List<PeerSocketAddress> _peerSocketAddresses = null;

        private int _keyCollectionSize = -1;
        private KeyCollection _keyCollection = null;

        private int _mapSize = -1;
        private DataMap _dataMap = null;
        private Data _data = null;
        private Number640 _key = null;

        private int _keyMap640KeysSize = -1;
        private KeyMap640Keys _keyMap640Keys = null;

        private int _keyMapByteSize = -1;
        private KeyMapByte _keyMapByte = null;

        private int _bufferSize = -1;
        private DataBuffer _buffer = null;

        private int _trackerDataSize = -1;
        private TrackerData _trackerData = null;
        private Data _currentTrackerData = null;

        private readonly ISignatureFactory _signatureFactory;

        public Decoder(ISignatureFactory signatureFactory)
        {
            _signatureFactory = signatureFactory;
        }

        public bool Decode(ChannelHandlerContext ctx, AlternativeCompositeByteBuf buffer, IPEndPoint recipient, IPEndPoint sender)
        {
            Logger.Debug("Decoding of TomP2P starts now. Readable: {0}.", buffer.ReadableBytes);

            try
            {
                long readerBefore = buffer.ReaderIndex;
                // TODO set sender for handling timeout?

                if (Message == null)
                {
                    bool doneHeader = DecodeHeader(buffer, recipient, sender);
                    if (doneHeader)
                    {
                        // TODO store the sender as an attribute??

                        Message.SetIsUdp(ctx.Channel.IsUdp);
                        if (Message.IsFireAndForget() && Message.IsUdp)
                        {
                            TimeoutFactory.RemoveTimeout(ctx);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                bool donePayload = DecodePayload(buffer);
                DecodeSignature(buffer, readerBefore, donePayload);
                return donePayload;
            }
            catch (Exception ex)
            {
                ctx.FireExceptionCaught(ex);
                Console.WriteLine(ex.ToString());
                return true;
            }
        }

        public Message PrepareFinish()
        {
            Message ret = Message;
            Message.SetDone();

            _contentTypes.Clear();
            Message = null;
            _neighborSize = -1;
            _neighborSet = null;
            // TODO set peerSocketAddressSize/peerSocketAddresses -1/null?
            _keyCollectionSize = -1;
            _keyCollection = null;
            _mapSize = -1;
            _dataMap = null;
            _data = null;
            // TODO set _key to null?
            _keyMap640KeysSize = -1;
            _keyMap640Keys = null;
            // TODO set _keyMapBytesSize/list to -1/null?
            _bufferSize = -1;
            _buffer = null;
            // TODO set _trackerDataSize/list to -1/null?
            // TODO set _signatureFactory to null?

            return ret;
        }

        private bool DecodeHeader(AlternativeCompositeByteBuf buffer, IPEndPoint recipient, IPEndPoint sender)
        {
            if (Message == null)
            {
                if (buffer.ReadableBytes < MessageHeaderCodec.HeaderSize)
                {
                    // we don't have the header yet, we need the full header first
                    // wait for more data
                    return false;
                }

                Message = MessageHeaderCodec.DecodeHeader(buffer, recipient, sender);
                // we have set the content types already
                Message.SetPresetContentTypes(true);

                foreach (var content in Message.ContentTypes)
                {
                    if (content == Message.Content.Empty)
                    {
                        break;
                    }
                    if (content == Message.Content.PublicKeySignature)
                    {
                        Message.SetHintSign();
                    }
                    _contentTypes.Enqueue(content);
                }
                Logger.Debug("Parsed message {0}.", Message);
                return true;
            }
            return false;
        }

        private bool DecodePayload(AlternativeCompositeByteBuf buffer)
        {
            Logger.Debug("About to pass message {0} to {1}. Buffer to read: {2}.", Message, Message.SenderSocket, buffer.ReadableBytes);

            if (!Message.HasContent())
            {
                return true;
            }

            int size;
            IPublicKey receivedPublicKey;

            while (_contentTypes.Count > 0)
            {
                Message.Content content = _contentTypes.Peek();
                Logger.Debug("Go for content: {0}.", content);

                switch (content)
                {
                    case Message.Content.Integer:
                        if (buffer.ReadableBytes < Utils.Utils.IntegerByteSize)
                        {
                            return false;
                        }
                        Message.SetIntValue(buffer.ReadInt());
                        LastContent = _contentTypes.Dequeue();
                        break;
                    case Message.Content.Long:
                        if (buffer.ReadableBytes < Utils.Utils.LongByteSize)
                        {
                            return false;
                        }
                        Message.SetLongValue(buffer.ReadLong());
                        LastContent = _contentTypes.Dequeue();
                        break;
                    case Message.Content.Key:
                        if (buffer.ReadableBytes < Number160.ByteArraySize)
                        {
                            return false;
                        }
                        var keyBytes = new sbyte[Number160.ByteArraySize];
                        buffer.ReadBytes(keyBytes);
                        Message.SetKey(new Number160(keyBytes));
                        LastContent = _contentTypes.Dequeue();
                        break;
                    case Message.Content.BloomFilter:
                        if (buffer.ReadableBytes < Utils.Utils.ShortByteSize)
                        {
                            return false;
                        }
                        size = buffer.GetUShort(buffer.ReaderIndex);
                        if (buffer.ReadableBytes < size)
                        {
                            return false;
                        }
                        Message.SetBloomFilter(new SimpleBloomFilter<Number160>(buffer));
                        LastContent = _contentTypes.Dequeue();
                        break;
                    case Message.Content.SetNeighbors:
                        if (_neighborSize == -1 && buffer.ReadableBytes < Utils.Utils.ByteByteSize)
                        {
                            return false;
                        }
                        if (_neighborSize == -1)
                        {
                            _neighborSize = buffer.ReadByte();
                        }
                        if (_neighborSet == null)
                        {
                            _neighborSet = new NeighborSet(-1, new List<PeerAddress>(_neighborSize));
                        }
                        for (int i = _neighborSet.Size; i < _neighborSize; i++)
                        {
                            if (buffer.ReadableBytes < Utils.Utils.ShortByteSize)
                            {
                                return false;
                            }
                            int header = buffer.GetUShort(buffer.ReaderIndex);
                            size = PeerAddress.CalculateSize(header);
                            if (buffer.ReadableBytes < size)
                            {
                                return false;
                            }
                            var pa = new PeerAddress(buffer);
                            _neighborSet.Add(pa);
                        }
                        Message.SetNeighborSet(_neighborSet);
                        LastContent = _contentTypes.Dequeue();
                        _neighborSize = -1; // TODO why here? not in prepareFinish()?
                        _neighborSet = null;
                        break;
                    case Message.Content.SetPeerSocket:
                        if (_peerSocketAddressSize == -1 && buffer.ReadableBytes < Utils.Utils.ByteByteSize)
                        {
                            return false;
                        }
                        if (_peerSocketAddressSize == -1)
                        {
                            _peerSocketAddressSize = buffer.ReadUByte();
                        }
                        if (_peerSocketAddresses == null)
                        {
                            _peerSocketAddresses = new List<PeerSocketAddress>(_peerSocketAddressSize);
                        }
                        for (int i = _peerSocketAddresses.Count; i < _peerSocketAddressSize; i++)
                        {
                            if (buffer.ReadableBytes < Utils.Utils.ByteByteSize)
                            {
                                return false;
                            }
                            int header = buffer.GetUByte(buffer.ReaderIndex);
                            bool isIPv4 = header == 0; // TODO check if works
                            size = PeerSocketAddress.Size(isIPv4);
                            if (buffer.ReadableBytes < size + Utils.Utils.ByteByteSize)
                            {
                                return false;
                            }
                            // skip the ipv4/ipv6 header
                            buffer.SkipBytes(1);
                            _peerSocketAddresses.Add(PeerSocketAddress.Create(buffer, isIPv4));
                        }
                        Message.SetPeerSocketAddresses(_peerSocketAddresses);
                        LastContent = _contentTypes.Dequeue();
                        _peerSocketAddressSize = -1; // TODO why here? not in prepareFinish()?
                        _peerSocketAddresses = null;
                        break;
                    case Message.Content.SetKey640:
                        if (_keyCollectionSize == -1 && buffer.ReadableBytes < Utils.Utils.IntegerByteSize)
                        {
                            return false;
                        }
                        if (_keyCollectionSize == -1)
                        {
                            _keyCollectionSize = buffer.ReadInt();
                        }
                        if (_keyCollection == null)
                        {
                            _keyCollection = new KeyCollection(new List<Number640>(_keyCollectionSize));
                        }
                        for (int i = _keyCollection.Size; i < _keyCollectionSize; i++)
                        {
                            if (buffer.ReadableBytes < 4 * Number160.ByteArraySize)
                            {
                                return false;
                            }
                            var me = new sbyte[Number160.ByteArraySize];

                            buffer.ReadBytes(me);
                            var locationKey = new Number160(me);

                            buffer.ReadBytes(me);
                            var domainKey = new Number160(me);

                            buffer.ReadBytes(me);
                            var contentKey = new Number160(me);

                            buffer.ReadBytes(me);
                            var versionKey = new Number160(me);

                            _keyCollection.Add(new Number640(locationKey, domainKey, contentKey, versionKey));
                        }
                        Message.SetKeyCollection(_keyCollection);
                        LastContent = _contentTypes.Dequeue();
                        _keyCollectionSize = -1; // TODO why here? not in prepareFinish()?
                        _keyCollection = null;
                        break;
                    case Message.Content.MapKey640Data:
                        if (_mapSize == -1 && buffer.ReadableBytes < Utils.Utils.IntegerByteSize)
                        {
                            return false;
                        }
                        if (_mapSize == -1)
                        {
                            _mapSize = buffer.ReadInt();
                        }
                        if (_dataMap == null)
                        {
                            _dataMap = new DataMap(new Dictionary<Number640, Data>(2 * _mapSize));
                        }
                        if (_data != null)
                        {
                            if (!_data.DecodeBuffer(buffer))
                            {
                                return false;
                            }
                            if (!_data.DecodeDone(buffer, Message.PublicKey(0), _signatureFactory))
                            {
                                return false;
                            }
                            _data = null; // TODO why here? not in prepareFinish()?
                            _key = null;
                        }
                        for (int i = _dataMap.Size; i < _mapSize; i++)
                        {
                            if (_key == null)
                            {
                                if (buffer.ReadableBytes < 4 * Number160.ByteArraySize)
                                {
                                    return false;
                                }
                                var me = new sbyte[Number160.ByteArraySize];
                                buffer.ReadBytes(me);
                                var locationKey = new Number160(me);
                                buffer.ReadBytes(me);
                                var domainKey = new Number160(me);
                                buffer.ReadBytes(me);
                                var contentKey = new Number160(me);
                                buffer.ReadBytes(me);
                                var versionKey = new Number160(me);

                                _key = new Number640(locationKey, domainKey, contentKey, versionKey);
                            }
                            _data = Data.DeocdeHeader(buffer, _signatureFactory);
                            if (_data == null)
                            {
                                return false;
                            }
                            _dataMap.BackingDataMap.Add(_key, _data);

                            if (!_data.DecodeBuffer(buffer))
                            {
                                return false;
                            }
                            if (!_data.DecodeDone(buffer, Message.PublicKey(0), _signatureFactory))
                            {
                                return false;
                            }
                            // if we have signed the message, set the public key anyway, but only if we indicated so
                            if (Message.IsSign && Message.PublicKey(0) != null && _data.HasPublicKey
                                && (_data.PublicKey == null || _data.PublicKey == PeerBuilder.EmptyPublicKey))
                            // TODO check empty key condition
                            {
                                _data.SetPublicKey(Message.PublicKey(0));
                            }
                            _data = null; // TODO why here? not in prepareFinish()?
                            _key = null;
                        }

                        Message.SetDataMap(_dataMap);
                        LastContent = _contentTypes.Dequeue();
                        _mapSize = -1; // TODO why here? not in prepareFinish()?
                        _dataMap = null;
                        break;
                    case Message.Content.MapKey640Keys:
                        if (_keyMap640KeysSize == -1 && buffer.ReadableBytes < Utils.Utils.IntegerByteSize)
                        {
                            return false;
                        }
                        if (_keyMap640KeysSize == -1)
                        {
                            _keyMap640KeysSize = buffer.ReadInt();
                        }
                        if (_keyMap640Keys == null)
                        {
                            _keyMap640Keys = new KeyMap640Keys(new SortedDictionary<Number640, ICollection<Number160>>());
                            // TODO check TreeMap equivalent
                        }

                        const int meta = 4 * Number160.ByteArraySize;

                        for (int i = _keyMap640Keys.Size; i < _keyMap640KeysSize; i++)
                        {
                            if (buffer.ReadableBytes < meta + Utils.Utils.ByteByteSize)
                            {
                                return false;
                            }
                            size = buffer.GetUByte(buffer.ReaderIndex + meta);

                            if (buffer.ReadableBytes <
                                meta + Utils.Utils.ByteByteSize + (size * Number160.ByteArraySize))
                            {
                                return false;
                            }
                            var me = new sbyte[Number160.ByteArraySize];
                            buffer.ReadBytes(me);
                            var locationKey = new Number160(me);
                            buffer.ReadBytes(me);
                            var domainKey = new Number160(me);
                            buffer.ReadBytes(me);
                            var contentKey = new Number160(me);
                            buffer.ReadBytes(me);
                            var versionKey = new Number160(me);

                            int numBasedOn = buffer.ReadByte();
                            var value = new HashSet<Number160>();
                            for (int j = 0; j < numBasedOn; j++)
                            {
                                buffer.ReadBytes(me);
                                var basedOnKey = new Number160(me);
                                value.Add(basedOnKey);
                            }

                            _keyMap640Keys.Put(new Number640(locationKey, domainKey, contentKey, versionKey), value);
                        }

                        Message.SetKeyMap640Keys(_keyMap640Keys);
                        LastContent = _contentTypes.Dequeue();
                        _keyMap640KeysSize = -1; // TODO why here? not in prepareFinish()?
                        _keyMap640Keys = null;
                        break;
                    case Message.Content.MapKey640Byte:
                        if (_keyMapByteSize == -1 && buffer.ReadableBytes < Utils.Utils.IntegerByteSize)
                        {
                            return false;
                        }
                        if (_keyMapByteSize == -1)
                        {
                            _keyMapByteSize = buffer.ReadInt();
                        }
                        if (_keyMapByte == null)
                        {
                            _keyMapByte = new KeyMapByte(new Dictionary<Number640, sbyte>(2 * _keyMapByteSize));
                        }

                        for (int i = _keyMapByte.Size; i < _keyMapByteSize; i++)
                        {
                            if (buffer.ReadableBytes < 4 * Number160.ByteArraySize + 1)
                            {
                                return false;
                            }
                            var me = new sbyte[Number160.ByteArraySize];
                            buffer.ReadBytes(me);
                            var locationKey = new Number160(me);
                            buffer.ReadBytes(me);
                            var domainKey = new Number160(me);
                            buffer.ReadBytes(me);
                            var contentKey = new Number160(me);
                            buffer.ReadBytes(me);
                            var versionKey = new Number160(me);

                            sbyte value = buffer.ReadByte();
                            _keyMapByte.Put(new Number640(locationKey, domainKey, contentKey, versionKey), value);
                        }

                        Message.SetKeyMapByte(_keyMapByte);
                        LastContent = _contentTypes.Dequeue();
                        _keyMapByteSize = -1; // TODO why here? not in prepareFinish()?
                        _keyMapByte = null;
                        break;
                    case Message.Content.ByteBuffer:
                        if (_bufferSize == -1 && buffer.ReadableBytes < Utils.Utils.IntegerByteSize)
                        {
                            return false;
                        }
                        if (_bufferSize == -1)
                        {
                            _bufferSize = buffer.ReadInt();
                        }
                        if (_buffer == null)
                        {
                            _buffer = new DataBuffer();
                        }

                        int already = _buffer.AlreadyTransferred;
                        int remaining = _bufferSize - already;
                        // already finished
                        if (remaining != 0)
                        {
                            int read = _buffer.TransferFrom(buffer, remaining);
                            if (read != remaining)
                            {
                                Logger.Debug(
                                    "Still looking for data. Indicating that its not finished yet. Already Transferred = {0}, Size = {1}.",
                                    _buffer.AlreadyTransferred, _bufferSize);
                                return false;
                            }
                        }

                        ByteBuf buf2 = AlternativeCompositeByteBuf.CompBuffer(_buffer.ToByteBufs());

                        Message.SetBuffer(new Buffer(buf2, _bufferSize));
                        LastContent = _contentTypes.Dequeue();
                        _bufferSize = -1;
                        _buffer = null;
                        break;
                    case Message.Content.SetTrackerData:
                        if (_trackerDataSize == -1 && buffer.ReadableBytes < Utils.Utils.ByteByteSize)
                        {
                            return false;
                        }
                        if (_trackerDataSize == -1)
                        {
                            _trackerDataSize = buffer.ReadUByte();
                        }
                        if (_trackerData == null)
                        {
                            _trackerData = new TrackerData(new Dictionary<PeerAddress, Data>(2 * _trackerDataSize));
                        }
                        if (_currentTrackerData != null)
                        {
                            if (!_currentTrackerData.DecodeBuffer(buffer))
                            {
                                return false;
                            }
                            if (!_currentTrackerData.DecodeDone(buffer, Message.PublicKey(0), _signatureFactory))
                            {
                                return false;
                            }
                            _currentTrackerData = null;
                        }
                        for (int i = _trackerData.Size; i < _trackerDataSize; i++)
                        {
                            if (buffer.ReadableBytes < Utils.Utils.ShortByteSize)
                            {
                                return false;
                            }

                            int header = buffer.GetUShort(buffer.ReaderIndex);
                            size = PeerAddress.CalculateSize(header);
                            if (buffer.ReadableBytes < Utils.Utils.ShortByteSize)
                            {
                                return false;
                            }
                            var pa = new PeerAddress(buffer);

                            _currentTrackerData = Data.DeocdeHeader(buffer, _signatureFactory);
                            if (_currentTrackerData == null)
                            {
                                return false;
                            }
                            _trackerData.PeerAddresses.Add(pa, _currentTrackerData);
                            if (Message.IsSign)
                            {
                                _currentTrackerData.SetPublicKey(Message.PublicKey(0));
                            }
                            if (!_currentTrackerData.DecodeBuffer(buffer))
                            {
                                return false;
                            }
                            if (!_currentTrackerData.DecodeDone(buffer, Message.PublicKey(0), _signatureFactory))
                            {
                                return false;
                            }
                            _currentTrackerData = null; // TODO why here?
                        }

                        Message.SetTrackerData(_trackerData);
                        LastContent = _contentTypes.Dequeue();
                        _trackerDataSize = -1;
                        _trackerData = null;
                        break;
                    case Message.Content.PublicKey: // fall-through
                    case Message.Content.PublicKeySignature:
                        receivedPublicKey = _signatureFactory.DecodePublicKey(buffer);
                        if (content == Message.Content.PublicKeySignature)
                        {
                            if (receivedPublicKey == PeerBuilder.EmptyPublicKey) // TODO check if works
                            {
                                // TODO throw InvalidKeyException
                                throw new SystemException("The public key cannot be empty.");
                            }
                        }
                        if (receivedPublicKey == null)
                        {
                            return false;
                        }

                        Message.SetPublicKey(receivedPublicKey);
                        LastContent = _contentTypes.Dequeue();
                        break;
                    default:
                        break;
                }
            }

            if (Message.IsSign)
            {
                var signatureEncode = _signatureFactory.SignatureCodec;
                size = signatureEncode.SignatureSize;
                if (buffer.ReadableBytes < size)
                {
                    return false;
                }

                signatureEncode.Read(buffer);
                Message.SetReceivedSignature(signatureEncode);
            }
            return true;
        }

        private void DecodeSignature(AlternativeCompositeByteBuf buffer, long readerBefore, bool donePayload)
        {
            var readerAfter = buffer.ReaderIndex;
            var len = readerAfter - readerBefore;
            if (len > 0)
            {
                VerifySignature(buffer, readerBefore, len, donePayload);
            }
        }

        private void VerifySignature(AlternativeCompositeByteBuf buffer, long readerBefore, long len, bool donePayload) // TODO throw exceptions?
        {
            if (!Message.IsSign)
            {
                return;
            }

            // if we read the complete data, we also read the signature
            // for the verification, we should not used this for the signature
            var length = donePayload ? len - (Number160.ByteArraySize + Number160.ByteArraySize) : len;
            MemoryStream[] byteBuffers = null; // TODO no clue how to port this

            var signature = _signatureFactory.Update(Message.PublicKey(0), byteBuffers); // TODO what's going on here?

            if (donePayload)
            {
                byte[] signatureReceived = Message.ReceivedSignature.Encode();
                if (true) // TODO implement .NET signature verification
                {
                    // set the public key only if the signature is correct
                    Message.SetVerified();
                    Logger.Debug("Signature check OK.");
                }
                else
                {
                    Logger.Warn("Signature check NOT OK. Message: {0}.", Message);
                }
            }
        }
    }
}
