﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Core.Message
{
    // TODO rename setters for payload field to -> "Add..."
    // TODO introduce properties instead of methods
    /// <summary>
    /// The message is in binary format in TomP2P. It has several header and payload fields.
    /// Since the serialization/encoding is done manually, no serialization field is needed.
    /// </summary>
    public class Message
    {
        // used for creating random message id
        private static readonly Random Random = new Random();

        public const int ContentTypeLength = 8;

        /// <summary>
        /// 8 x 4 bit.
        /// </summary>
        public enum Content
        {
            Empty, Key, MapKey640Data, MapKey640Keys, SetKey640, SetNeighbors, ByteBuffer,
            Long, Integer, PublicKeySignature, SetTrackerData, BloomFilter, MapKey640Byte,
            PublicKey, SetPeerSocket, User1
        }

        /// <summary>
        /// 4 x 1 bit.
        /// </summary>
        public enum MessageType
        {
            // REQUEST_1 is the normal request
            // REQUEST_2 for GET returns the extended digest (hashes of all stored data)
            // REQUEST_3 for GET returns a Bloom filter
            // REQUEST_4 for GET returns a range (min/max)
            // REQUEST_2 for PUT/ADD/COMPARE_PUT means protect domain
            // REQUEST_3 for PUT means put if absent
            // REQUEST_3 for COMPARE_PUT means partial (partial means that put those data that match compare, ignore others)
            // REQUEST_4 for PUT means protect domain and put if absent
            // REQUEST_4 for COMPARE_PUT means partial and protect domain
            // REQUEST_2 for REMOVE means send back results
            // REQUEST_2 for RAW_DATA means serialize object
            // *** NEIGHBORS has four different cases
            // REQUEST_1 for NEIGHBORS means check for put (no digest) for tracker and storage
            // REQUEST_2 for NEIGHBORS means check for get (with digest) for storage
            // REQUEST_3 for NEIGHBORS means check for get (with digest) for tracker
            // REQUEST_4 for NEIGHBORS means check for put (with digest) for task
            // REQUEST_FF_1 for PEX means fire and forget, coming from mesh
            // REQUEST_FF_1 for PEX means fire and forget, coming from primary
            // REQUEST_1 for TASK is submit new task
            // REQUEST_2 for TASK is status
            // REQUEST_3 for TASK is send back result
            Request1, Request2, Request3, Request4, RequestFf1, RequestFf2, Ok,
            PartiallyOk, NotFound, Denied, UnknownId, Exception, Cancel, User1, User2
        }

        // *** HEADER ***

        /// <summary>
        /// Randomly generated message ID.
        /// </summary>
        public int MessageId { get; private set; }

        /// <summary>
        /// Returns the version, which is 32bit. Each application can choose a version to not interfere with other applications.
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Determines if its a request or command reply and what kind of reply (error, warning states).
        /// </summary>
        public MessageType Type { get; private set; }

        /* commands so far:
         *  0: PING
         *  1: PUT
         *  2: GET
         *  3: ADD
         *  4: REMOVE
         *  5: NEIGHBORS
         *  6: QUIT
         *  7: DIRECT_DATA
         *  8: TRACKER_ADD
         *  9: TRACKER_GET
         * 10: PEX
         * 11: TASK
         * 12: BROADCAST_DATA*/

        /// <summary>
        /// Command of the message, such as GET, PING, etc.
        /// </summary>
        public sbyte Command { get; private set; } // TODO check if should be unsigned

        /// <summary>
        /// The ID of the sender. Note that the IP is set via the socket.
        /// </summary>
        public PeerAddress Sender { get; private set; }

        /// <summary>
        /// The ID of the recipient. Note that the IP is set via the socket.
        /// </summary>
        public PeerAddress Recipient { get; private set; }

        public PeerAddress RecipientRelay { get; private set; } // TODO make transient

        /// <summary>
        /// The options from the last byte of the header.
        /// </summary>
        public int Options { get; private set; }

        // *** PAYLOAD ***

        // we can send 8 types

        /// <summary>
        /// The content types. Can be empty if not set.
        /// </summary>
        public Content[] ContentTypes { get; private set; }

        /// <summary>
        /// The serialized content and references to the respective arrays.
        /// </summary>
        public Queue<MessageContentIndex> ContentReferences { get; private set; }

        // following the payload objects:
        // content lists:
        private List<NeighborSet> _neighborsList = null;
        private List<Number160> _keyList = null; 
        private List<SimpleBloomFilter<Number160>> _bloomFilterList = null;
        private List<DataMap> _dataMapList = null;
        //private IPublicKey _publicKey = null; // there can only be one
        private List<int> _integerList = null;
        private List<long> _longList = null;
        private List<KeyCollection> _keyCollectionList = null;
        private List<KeyMap640Keys> _keyMap640KeysList = null;
        private List<KeyMapByte> _keyMapByteList = null;
        private List<Buffer> _bufferList = null;
        private List<TrackerData> _trackerDataList = null;
        private List<IPublicKey> _publicKeyList = null; 
        private List<PeerSocketAddress> _peerSocketAddressList = null;
        public ISignatureCodec ReceivedSignature { get; private set; }

        //TODO make status variables transient
        // status variables: 
        private bool _presetContentTypes = false;
        public IPrivateKey PrivateKey { get; private set; }
        /// <summary>
        /// The sender of the packet. This is needed for UDP traffic.
        /// </summary>
        public IPEndPoint SenderSocket { get; private set; }
        /// <summary>
        /// The recipient as we saw it on the interface. This is needed for UDP (especially broadcast) packets.
        /// </summary>
        public IPEndPoint RecipientSocket { get; private set; }
        public bool IsUdp { get; private set; }
        public bool IsDone { get; private set; }
        private bool _sign = false;
        private bool _content = false;
        public bool Verified { get; private set; }

        /// <summary>
        /// Creates a message with a random message ID.
        /// </summary>
        public Message()
        {
            IsUdp = false;
            IsDone = false;
            ReceivedSignature = null;
            MessageId = Random.Next();
            ContentTypes = new Content[ContentTypeLength];
            ContentReferences = new Queue<MessageContentIndex>();
        }

        /// <summary>
        /// For deserialization, the ID needs to be set.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <returns>This class.</returns>
        public Message SetMessageId(int messageId)
        {
            MessageId = messageId;
            return this;
        }

        /// <summary>
        /// For deserialization.
        /// </summary>
        /// <param name="version">The 24bit version.</param>
        /// <returns>This class.</returns>
        public Message SetVersion(int version)
        {
            Version = version;
            return this;
        }

        /// <summary>
        /// Set the message type. Either its a request or reply (with error and warning codes).
        /// </summary>
        /// <param name="type">The type of the message.</param>
        /// <returns>This class.</returns>
        public Message SetType(MessageType type)
        {
            Type = type;
            return this;
        }

        /// <summary>
        /// Command of the message, such as GET, PING, etc.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>This class.</returns>
        public Message SetCommand(sbyte command)
        {
            Command = command;
            return this;
        }

        /// <summary>
        /// Set the ID of the sender. The IP of the sender will NOT be transferred, as this information is in the IP packet.
        /// </summary>
        /// <param name="sender">The ID of the sender.</param>
        /// <returns>This class.</returns>
        public Message SetSender(PeerAddress sender)
        {
            Sender = sender;
            return this;
        }

        /// <summary>
        /// Set the ID of the recipient. The IP is used to connect to the recipient, but the IP is NOT transferred.
        /// </summary>
        /// <param name="recipient">The ID of the recipient.</param>
        /// <returns>This class.</returns>
        public Message SetRecipient(PeerAddress recipient)
        {
            Recipient = recipient;
            return this;
        }

        public Message SetRecipientRelay(PeerAddress recipientRelay)
        {
            RecipientRelay = recipientRelay;
            return this;
        }

        /// <summary>
        /// Convenient method to set the content type.
        /// Set first content type 1, if this is set (not empty), then set the second, etc. 
        /// </summary>
        /// <param name="contentType">The content type to set.</param>
        /// <returns>This class.</returns>
        public Message SetContentType(Content contentType)
        {
            for (int i = 0, reference = 0; i < ContentTypeLength; i++)
            {
                if (ContentTypes[i] == Content.Empty) // TODO check if this works as expected (cf. java tests for null)
                {
                    if (contentType == Content.PublicKeySignature && i != 0)
                    {
                        throw new InvalidOperationException("The public key needs to be the first to be set.");
                    }
                    ContentTypes[i] = contentType;
                    ContentReferences.Enqueue(new MessageContentIndex(reference, contentType));
                    return this;
                }
                if (ContentTypes[i] == contentType)
                {
                    reference++;
                }
                else if (ContentTypes[i] == Content.PublicKeySignature || ContentTypes[i] == Content.PublicKey)
                {
                    // special handling for public key, as we store both in the same list
                    if (contentType == Content.PublicKeySignature || contentType == Content.PublicKey)
                    {
                        reference++;
                    }
                }
            }
            throw new InvalidOperationException("Already set 8 content types.");
        }

        /// <summary>
        /// Sets or replaces the content type at a specific index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="contentType">The content type to be set.</param>
        /// <returns>This class.</returns>
        public Message SetContentType(int index, Content contentType)
        {
            ContentTypes[index] = contentType;
            return this;
        }

        /// <summary>
        /// Used for deserialization.
        /// </summary>
        /// <param name="contentTypes">The content types that were decoded.</param>
        /// <returns>This class.</returns>
        public Message SetContentType(Content[] contentTypes)
        {
            ContentTypes = contentTypes;
            return this;
        }

        // TODO check comment, if only vs. only if
        /// <summary>
        /// Restore the content references if only the content types array is
        /// present. The content references are removed when decoding a message. That
        /// means if a message was received it cannot be used a second time as the
        /// content references are not there anymore. This method restores the
        /// content references based on the content types of the message.
        /// </summary>
        public void RestoreContentReferences()
        {
            IDictionary<Content, int> references = new Dictionary<Content, int>(ContentTypes.Count() * 2);

            foreach (var contentType in ContentTypes)
            {
                // TODO what about Content.Undefined
                if (contentType == Content.Empty)
                {
                    return;
                }

                int index = 0;
                if (contentType == Content.PublicKeySignature || contentType == Content.PublicKey)
                {
                    int j = references[Content.PublicKeySignature];
                    if (j != default(int))
                    {
                        index = j;
                    }
                    else
                    {
                        j = references[Content.PublicKey];
                        if (j != default(int))
                        {
                            index = j;
                        }
                    }
                }

                if (!references.ContainsKey(contentType))
                {
                    references.Add(contentType, index);
                }
                else
                {
                    index = references[contentType];
                }

                ContentReferences.Enqueue(new MessageContentIndex(index, contentType));
                references.Add(contentType, index + 1);
            }
        }

        /// <summary>
        /// True if we have content and not only the header.
        /// </summary>
        public bool HasContent()
        {
            return ContentReferences.Count > 0 || _content;
        }
        
        /// <summary>
        /// We can set this already in the header to know if we have content or not.
        /// </summary>
        /// <param name="content"></param>
        /// <returns>This class.</returns>
        public Message HasContent(bool content)
        {
            _content = content;
            return this;
        }

        // *** TYPES OF REQUEST ***

        // TODO check comment
        // TODO make property
        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if this is a request, a regural or a fire and forget.</returns>
        public bool IsRequest()
        {
            return Type == MessageType.Request1 || Type == MessageType.Request2 || Type == MessageType.Request3
                || Type == MessageType.Request4 || Type == MessageType.RequestFf1 || Type == MessageType.RequestFf2;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if this is a fire and forget. (That means, we don't expect an answer.)</returns>
        public bool IsFireAndForget()
        {
            return Type == MessageType.RequestFf1 || Type == MessageType.RequestFf2;
        }

        /// <summary>
        /// True if the message was OK, or at least sent partial data.
        /// </summary>
        /// <returns>True if the message was OK, or at least sent partial data.</returns>
        public bool IsOk()
        {
            return Type == MessageType.Ok || Type == MessageType.PartiallyOk;
        }

        /// <summary>
        /// True if the message arrived, but data was not found or access was denied.
        /// </summary>
        /// <returns>True if the message arrived, but data was not found or access was denied.</returns>
        public bool IsNotOk()
        {
            return Type == MessageType.NotFound || Type == MessageType.Denied;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if the message contained an unexpected error or behavior.</returns>
        public bool IsError()
        {
            return IsError(Type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">The message type to check.</param>
        /// <returns>True if the message contained an unexpected error or behavior.</returns>
        public bool IsError(MessageType type)
        {
            return type == MessageType.UnknownId || type == MessageType.Exception || type == MessageType.Cancel;
        }

        /// <summary>
        /// Set the option from the last byte of the header.
        /// </summary>
        /// <param name="option">The option to be set.</param>
        /// <returns>This class.</returns>
        public Message SetOptions(int option)
        {
            Options = option;
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isKeepAlive">True if the connection should remain open. We need to announce this in the header, as otherwise the other end has an idle handler that will close the connection.</param>
        /// <returns>This class.</returns>
        public Message SetKeepAlive(bool isKeepAlive)
        {
            if (isKeepAlive)
            {
                Options |= 1;
            }
            else
            {
                Options &= ~1;
            }
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True, if this message was sent on a connection that should be kept alive.</returns>
        public bool IsKeepAlive()
        {
            return (Options & 1) > 0;
        }

        public Message SetStreaming()
        {
            return SetStreaming(true);
        }

        public Message SetStreaming(bool isStreaming)
        {
            if (isStreaming)
            {
                Options |= 2;
            }
            else
            {
                Options &= ~2;
            }
            return this;
        }

        public bool IsStreaming()
        {
            return (Options & 2) > 0;
        }

        // Header data ends here.
        // Static payload starts now.

        public Message SetKey(Number160 key)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.Key);
            }
            if (_keyList == null)
            {
                _keyList = new List<Number160>(1);
            }
            _keyList.Add(key);
            return this;
        }

        public List<Number160> KeyList
        {
            get { return _keyList ?? new List<Number160>(); }
        }

        public Number160 Key(int index)
        {
            if (_keyList == null || index > _keyList.Count - 1)
            {
                return null;
            }
            return _keyList[index];
        }

        public Message SetBloomFilter(SimpleBloomFilter<Number160> bloomFilter)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.BloomFilter);
            }
            if (_bloomFilterList == null)
            {
                _bloomFilterList = new List<SimpleBloomFilter<Number160>>(1);
            }
            _bloomFilterList.Add(bloomFilter);
            return this;
        }

        public List<SimpleBloomFilter<Number160>> BloomFilterList
        {
            get { return _bloomFilterList ?? new List<SimpleBloomFilter<Number160>>(); }
        }

        public SimpleBloomFilter<Number160> BloomFilter(int index)
        {
            if (_bloomFilterList == null || index > _bloomFilterList.Count - 1)
            {
                return null;
            }
            return _bloomFilterList[index];
        }

        public Message SetPublicKeyAndSign(KeyPair keyPair)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.PublicKeySignature);
            }
            SetPublicKey0(keyPair.PublicKey);
            PrivateKey = keyPair.PrivateKey;

            return this;
        }

        public Message SetIntValue(int integer)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.Integer);
            }
            if (_integerList == null)
            {
                _integerList = new List<int>(1);
            }
            _integerList.Add(integer);
            return this;
        }

        public List<int> IntList
        {
            get { return _integerList ?? new List<int>(); }
        }

        public int IntAt(int index)
        {
            if (_integerList == null || index > _integerList.Count - 1)
            {
                return 0; // TODO cannot return null
            }
            return _integerList[index];
        }

        public Message SetLongValue(long long0)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.Long);
            }
            if (_longList == null)
            {
                _longList = new List<long>(1);
            }
            _longList.Add(long0);
            return this;
        }

        public List<long> LongList
        {
            get { return _longList ?? new List<long>(); }
        }

        public long LongAt(int index)
        {
            if (_longList == null || index > _longList.Count - 1)
            {
                return 0; // TODO cannot return null
            }
            return _longList[index];
        }

        public Message SetNeighborSet(NeighborSet neighborSet)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.SetNeighbors);
            }
            if (_neighborsList == null)
            {
                _neighborsList = new List<NeighborSet>(1);
            }
            _neighborsList.Add(neighborSet);
            return this;
        }

        public List<NeighborSet> NeighborSetList
        {
            get { return _neighborsList ?? new List<NeighborSet>(); }
        }

        public NeighborSet NeighborsSet(int index)
        {
            if (_neighborsList == null || index > _neighborsList.Count - 1)
            {
                return null;
            }
            return _neighborsList[index];
        }

        public Message SetDataMap(DataMap dataMap)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.MapKey640Data);
            }
            if (_dataMapList == null)
            {
                _dataMapList = new List<DataMap>(1);
            }
            _dataMapList.Add(dataMap);
            return this;
        }

        public List<DataMap> DataMapList
        {
            get { return _dataMapList ?? new List<DataMap>(); }
        }

        public DataMap DataMap(int index)
        {
            if (_dataMapList == null || index > _dataMapList.Count - 1)
            {
                return null;
            }
            return _dataMapList[index];
        }

        public Message SetKeyCollection(KeyCollection keyCollection)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.SetKey640);
            }
            if (_keyCollectionList == null)
            {
                _keyCollectionList = new List<KeyCollection>();
            }
            _keyCollectionList.Add(keyCollection);
            return this;
        }

        public List<KeyCollection> KeyCollectionList
        {
            get { return _keyCollectionList ?? new List<KeyCollection>(); }
        }

        public KeyCollection KeyCollection(int index)
        {
            if (_keyCollectionList == null || index > _keyCollectionList.Count - 1)
            {
                return null;
            }
            return _keyCollectionList[index];
        }

        public Message SetKeyMap640Keys(KeyMap640Keys keyMap640Keys)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.MapKey640Keys);
            }
            if (_keyMap640KeysList == null)
            {
                _keyMap640KeysList = new List<KeyMap640Keys>(1);
            }
            _keyMap640KeysList.Add(keyMap640Keys);
            return this;
        }

        public List<KeyMap640Keys> KeyMap640KeysList
        {
            get { return _keyMap640KeysList ?? new List<KeyMap640Keys>(); }
        }

        public KeyMap640Keys KeyMap640Keys(int index)
        {
            if (_keyMap640KeysList == null || index > _keyMap640KeysList.Count - 1)
            {
                return null;
            }
            return _keyMap640KeysList[index];
        }

        public Message SetKeyMapByte(KeyMapByte keyMapByte)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.MapKey640Byte);
            }
            if (_keyMapByteList == null) {
                _keyMapByteList = new List<KeyMapByte>(1);
            }
            _keyMapByteList.Add(keyMapByte);
            return this;
        }

        public List<KeyMapByte> KeyMapByteList
        {
            get { return _keyMapByteList ?? new List<KeyMapByte>(); }
        }

        public KeyMapByte KeyMapByte(int index)
        {
            if (_keyMapByteList == null || index > _keyMapByteList.Count - 1)
            {
                return null;
            }
            return _keyMapByteList[index];
        }

        public Message SetPublicKey(IPublicKey publicKey)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.PublicKey);
            }
            if (_publicKeyList == null)
            {
                _publicKeyList = new List<IPublicKey>(1);
            }
            _publicKeyList.Add(publicKey);
            return this;
        }

        public Message SetPublicKey0(IPublicKey publicKey)
        {
            if (_publicKeyList == null)
            {
                _publicKeyList = new List<IPublicKey>(1);
            }
            _publicKeyList.Add(publicKey);
            return this;
        }

        public List<IPublicKey> PublicKeyList
        {
            get { return _publicKeyList ?? new List<IPublicKey>(); }
        }

        public IPublicKey PublicKey(int index)
        {
            if (_publicKeyList == null || index > _publicKeyList.Count - 1)
            {
                return null;
            }
            return _publicKeyList[index];
        }

        public Message SetPeerSocketAddresses(IEnumerable<PeerSocketAddress> peerSocketAddresses)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.SetPeerSocket);
            }
            var socketAddresses = peerSocketAddresses as IList<PeerSocketAddress> ?? peerSocketAddresses.ToList();
            if (_peerSocketAddressList == null)
            {
                _peerSocketAddressList = new List<PeerSocketAddress>(socketAddresses.Count());
            }
            _peerSocketAddressList.AddRange(socketAddresses);
            return this;
        }

        public List<PeerSocketAddress> PeerSocketAddresses
        {
            get { return _peerSocketAddressList ?? new List<PeerSocketAddress>(); }
        }

        public Message SetBuffer(Buffer byteBuffer)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.ByteBuffer);
            }
            if (_bufferList == null)
            {
                _bufferList = new List<Buffer>(1);
            }
            _bufferList.Add(byteBuffer);
            return this;
        }

        public List<Buffer> BufferList
        {
            get { return _bufferList ?? new List<Buffer>(); }
        }

        public Buffer Buffer(int index)
        {
            if (_bufferList == null || index > _bufferList.Count - 1)
            {
                return null;
            }
            return _bufferList[index];
        }

        public Message SetTrackerData(TrackerData trackerData)
        {
            if (!_presetContentTypes)
            {
                SetContentType(Content.SetTrackerData);
            }
            if (_trackerDataList == null)
            {
                _trackerDataList = new List<TrackerData>(1);
            }
            _trackerDataList.Add(trackerData);
            return this;
        }

        public List<TrackerData> TrackerDataList
        {
            get { return _trackerDataList ?? new List<TrackerData>(); }
        }

        public TrackerData TrackerData(int index)
        {
            if (_trackerDataList == null || index > _trackerDataList.Count - 1)
            {
                return null;
            }
            return _trackerDataList[index];
        }

        public Message SetReceivedSignature(ISignatureCodec signatureEncode)
        {
            ReceivedSignature = signatureEncode;
            return this;
        }

        // end of content payload

        // *** NON-TRANSFERABLE OBJECTS ***

        /// <summary>
        /// If we are setting values from the decoder, then the content type is already set.
        /// </summary>
        /// <param name="presetContentTypes">True if the content type is already set.</param>
        /// <returns>This class.</returns>
        public Message SetPresetContentTypes(bool presetContentTypes)
        {
            _presetContentTypes = presetContentTypes;
            return this;
        }

        /// <summary>
        /// Store the sender of the packet. This is needed for UDP traffic.
        /// </summary>
        /// <param name="senderSocket">The sender as we saw it on the interface.</param>
        /// <returns>This class.</returns>
        public Message SetSenderSocket(IPEndPoint senderSocket)
        {
            SenderSocket = senderSocket;
            return this;
        }

        /// <summary>
        /// Store the recipient of the packet. This is needed for UDP (especially broadcast) packets.
        /// </summary>
        /// <param name="recipientSocket">The recipient as we saw it on the interface.</param>
        /// <returns>This class.</returns>
        public Message SetRecipientSocket(IPEndPoint recipientSocket)
        {
            RecipientSocket = recipientSocket;
            return this;
        }

        
        /// <summary>
        /// Set if we have a signed message.
        /// </summary>
        /// <returns>This class.</returns>
        public Message SetHintSign()
        {
            _sign = true;
            return this;
        }

        /// <summary>
        /// True, if message is or should be signed.
        /// </summary>
        public bool IsSign
        {
            get { return _sign || PrivateKey != null; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isUdp">True, if connection is UDP.</param>
        /// <returns>This class.</returns>
        public Message SetIsUdp(bool isUdp)
        {
            IsUdp = isUdp;
            return this;
        }


        public Message SetVerified()
        {
            return SetVerified(true);
        }

        public Message SetVerified(bool isVerified)
        {
            Verified = isVerified;
            return this;
        }

        /// <summary>
        /// Set done to true, if message decoding or encoding is done.
        /// </summary>
        /// <returns>This class.</returns>
        public Message SetDone()
        {
            return SetDone(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isDone">True, if message decoding or encoding is done.</param>
        /// <returns>This class.</returns>
        public Message SetDone(bool isDone)
        {
            IsDone = true;
            return this;
        }

        public override string ToString()
        {
            var sb = new StringBuilder("msgid=");
            return sb.Append(MessageId)
                .Append(",t=").Append(Type)
                .Append(",c=").Append(Enum.GetName(typeof(Rpc.Rpc.Commands), Command))
                .Append(IsUdp ? ",udp" : ",tcp")
                .Append(",s=").Append(Sender)
                .Append(",r=").Append(Recipient).ToString();
        }
    }
}
