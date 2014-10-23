using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using NLog;
using TomP2P.Connection;
using TomP2P.Peers;
using TomP2P.Rpc;
using TomP2P.Storage;
using TomP2P.Workaround;

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

        private int _keyCollectionsSize = -1;
        private List<KeyCollection> _keyCollections = null;

        private int _mapSize = -1;
        private DataMap _dataMap = null;
        private Data _data = null;
        private Number640 _key = null;

        private int _keyMap640KeysSize = -1;
        private KeyMap640Keys _keyMap640Keys = null;

        private int _keyMapByteSize = -1;
        private KeyMapByte _keyMapByte = null;

        private int _bufferSize = -1;
        private Buffer _buffer = null;

        private int _trackerDataSize = -1;
        private TrackerData _trackerData = null;
        private Data _currentTrackerData = null;

        private readonly ISignatureFactory _signatureFactory;

        public Decoder(ISignatureFactory signatureFactory)
        {
            _signatureFactory = signatureFactory;
        }

        // TODO handle the Netty specific stuff, needed in .NET?
        public bool Decode(BinaryReader buffer, IPEndPoint recipient, IPEndPoint sender)
        {
            Logger.Debug("Decoding of TomP2P starts now. Readable: {0}.", buffer.ReadableBytes());

            try
            {
                // TODO review/redo: handle specific stuff
                long readerBefore = buffer.BaseStream.Position;

                // TODO set sender of this message for handling timeout??

                if (Message == null)
                {
                    bool doneHeader = DecodeHeader(buffer, recipient, sender);
                    if (doneHeader)
                    {
                        // TODO store the sender as an attribute??

                        Message.SetIsUdp(false); // TODO how to get whether it's UDP?
                        if (Message.IsFireAndForget() && Message.IsUdp)
                        {
                            // TODO remove timeout
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                bool donePayload = DecodePayload(buffer);
                DecodeSignature(buffer, readerBefore, donePayload);
                // TODO buffer.discardSomeReadBytes??
                return donePayload;
            }
            catch (Exception ex)
            {
                // TODO netty fire exception caught
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
            _keyCollectionsSize = -1;
            _keyCollections = null;
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

        private bool DecodeHeader(BinaryReader buffer, IPEndPoint recipient, IPEndPoint sender)
        {
            if (Message == null)
            {
                if (buffer.ReadableBytes() < MessageHeaderCodec.HeaderSize)
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

        private bool DecodePayload(BinaryReader buffer) // TODO throw exceptions?
        {
            Logger.Debug("About to pass message {0} to {1}. Buffer to read: {2}.", Message, Message.SenderSocket, buffer.ReadableBytes());

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
                        if (buffer.ReadableBytes() < Utils.Utils.IntegerByteSize)
                        {
                            return false;
                        }
                        Message.SetIntValue(buffer.ReadInt32());
                        LastContent = _contentTypes.Dequeue();
                        break;
                    case Message.Content.Long:
                        if (buffer.ReadableBytes() < Utils.Utils.LongByteSize)
                        {
                            return false;
                        }
                        Message.SetLongValue(buffer.ReadInt64());
                        LastContent = _contentTypes.Dequeue();
                        break;
                    case Message.Content.Key:
                        if (buffer.ReadableBytes() < Number160.ByteArraySize)
                        {
                            return false;
                        }
                        byte[] keyBytes = buffer.ReadBytes(Number160.ByteArraySize);
                        Message.SetKey(new Number160(keyBytes));
                        LastContent = _contentTypes.Dequeue();
                        break;
                    case Message.Content.BloomFilter:
                        if (buffer.ReadableBytes() < Utils.Utils.ShortByteSize)
                        {
                            return false;
                        }
                        // TODO check port
                        size = buffer.ReadUInt16(); // uses little-endian
                        Message.SetBloomFilter(new SimpleBloomFilter<Number160>(buffer));
                        LastContent = _contentTypes.Dequeue();
                        break;
                    case Message.Content.SetNeighbors:
                        if (_neighborSize == -1 && buffer.ReadableBytes() < Utils.Utils.ByteByteSize)
                        {
                            return false;
                        }
                        if (_neighborSize == -1)
                        {
                            _neighborSize = buffer.ReadByte(); // unsigned bytes // TODO byte -> int conversion valid?
                        }
                        if (_neighborSet == null)
                        {
                            _neighborSet = new NeighborSet(-1, new List<PeerAddress>(_neighborSize));
                        }
                        for (int i = _neighborSet.Size; i < _neighborSize; i++)
                        {
                            if (buffer.ReadableBytes() < Utils.Utils.ShortByteSize)
                            {
                                return false;
                            }
                            // TODO check port, java's getter don't change the reader index -> mimic behaviour
                            int header = buffer.ReadUInt16();
                            size = PeerAddress.CalculateSize(header);
                            if (buffer.ReadableBytes() < size)
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
                        if (_peerSocketAddressSize == -1 && buffer.ReadableBytes() < Utils.Utils.ByteByteSize)
                        {
                            return false;
                        }
                        if (_peerSocketAddresses == null)
                        {
                            _peerSocketAddresses = new List<PeerSocketAddress>(_peerSocketAddressSize);
                        }
                        for (int i = _peerSocketAddresses.Count; i < _peerSocketAddressSize; i++)
                        {
                            if (buffer.ReadableBytes() < Utils.Utils.ByteByteSize)
                            {
                                return false;
                            }
                            // TODO check port, java's getter don't change the reader index -> mimic behaviour
                            int header = buffer.ReadByte();
                            bool isIPv4 = header == 0; // TODO check if works
                            size = PeerSocketAddress.CalculateSize(isIPv4);
                            if (buffer.ReadableBytes() < size + Utils.Utils.ByteByteSize)
                            {
                                return false;
                            }
                            // skip the ipv4/ipv6 header
                            buffer.ReadByte();
                            _peerSocketAddresses.Add(PeerSocketAddress.Create(buffer, isIPv4));
                        }
                        Message.SetPeerSocketAddresses(_peerSocketAddresses);
                        LastContent = _contentTypes.Dequeue();
                        _peerSocketAddressSize = -1; // TODO why here? not in prepareFinish()?
                        _peerSocketAddresses = null;
                        break;
                }

                if (Message.IsSign)
                {
                    var signatureEncode = _signatureFactory.SignatureCodec;
                    size = signatureEncode.SignatureSize;
                    if (buffer.ReadableBytes() < size)
                    {
                        return false;
                    }

                    signatureEncode.Read(buffer);
                    Message.SetReceivedSignature(signatureEncode);
                }
            }
        }

        private void DecodeSignature(BinaryReader buffer, long readerBefore, bool donePayload)
        {
            var readerAfter = buffer.BaseStream.Position; // TODO readerIndex
            var len = readerAfter - readerBefore;
            if (len > 0)
            {
                VerifySignature(buffer, readerBefore, len, donePayload);
            }
        }

        private void VerifySignature(BinaryReader buffer, long readerBefore, long len, bool donePayload) // TODO throw exceptions?
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
