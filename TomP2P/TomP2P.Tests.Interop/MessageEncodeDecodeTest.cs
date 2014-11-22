using System.Net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;
using TomP2P.Message;
using TomP2P.Peers;
using TomP2P.Storage;
using Decoder = TomP2P.Message.Decoder;
using Buffer = TomP2P.Message.Buffer;

namespace TomP2P.Tests.Interop
{
    [TestFixture]
    public class MessageEncodeDecodeTest
    {
        /*Empty, Key, MapKey640Data, MapKey640Keys, SetKey640, SetNeighbors, ByteBuffer,
        Long, Integer, PublicKeySignature, SetTrackerData, BloomFilter, MapKey640Byte,
        PublicKey, SetPeerSocket, User1*/

        // 20 bytes (Number160 length)
        static sbyte[] _sampleBytes1 = new sbyte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        static sbyte[] _sampleBytes2 = new sbyte[] { 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        static sbyte[] _sampleBytes3 = new sbyte[Number160.ByteArraySize];

        static Number160 _sample160_1 = Number160.Zero;
        static Number160 _sample160_2 = Number160.One;
        static Number160 _sample160_3 = Number160.MaxValue;
        static Number160 _sample160_4 = new Number160(_sampleBytes1);
        static Number160 _sample160_5 = new Number160(_sampleBytes2);

        static Number640 _sample640_1 = Number640.Zero;
        static Number640 _sample640_2 = new Number640(new Number160(_sampleBytes1), new Number160(_sampleBytes2), new Number160(_sampleBytes3), Number160.MaxValue);
        static Number640 _sample640_3 = new Number640(Number160.MaxValue, new Number160(_sampleBytes3), new Number160(_sampleBytes2), new Number160(_sampleBytes1));

        static Data _sampleData1 = new Data(_sampleBytes1);
        static Data _sampleData2 = new Data(_sampleBytes1);
        static Data _sampleData3 = new Data(_sampleBytes1);

        [Test]
        public void TestMessageDecodeEmpty()
        {
            // create same message object as in Java
            var m1 = Utils2.CreateDummyMessage();

            // compare Java encoded and .NET decoded objects
            var m2 = DecodeMessage(JarRunner.RequestJavaBytes());

            Assert.IsTrue(CheckSameContentTypes(m1, m2));
        }

        [Test]
        public void TestMessageDecodeKey()
        {
            // create same message object as in Java
            var m1 = Utils2.CreateDummyMessage();

            m1.SetKey(_sample160_1);
            m1.SetKey(_sample160_2);
            m1.SetKey(_sample160_3);
            m1.SetKey(_sample160_4);
            m1.SetKey(_sample160_5);
            m1.SetKey(_sample160_1);
            m1.SetKey(_sample160_2);
            m1.SetKey(_sample160_3);

            // compare Java encoded and .NET decoded objects
            var m2 = DecodeMessage(JarRunner.RequestJavaBytes());

            Assert.IsTrue(CheckSameContentTypes(m1, m2));
            Assert.IsTrue(CheckIsSameList(m1.KeyList, m2.KeyList));
        }

        [Test]
        public void TestMessageDecodeMapKey640Data()
        {
            // create same message object as in Java
            IDictionary<Number640, Data> sampleMap1 = new Dictionary<Number640, Data>();
            sampleMap1.Add(_sample640_1, _sampleData1);
            sampleMap1.Add(_sample640_1, _sampleData2);
            sampleMap1.Add(_sample640_1, _sampleData3);

            IDictionary<Number640, Data> sampleMap2 = new Dictionary<Number640, Data>();
            sampleMap2.Add(_sample640_2, _sampleData1);
            sampleMap2.Add(_sample640_2, _sampleData2);
            sampleMap2.Add(_sample640_2, _sampleData3);

            IDictionary<Number640, Data> sampleMap3 = new Dictionary<Number640, Data>();
            sampleMap3.Add(_sample640_3, _sampleData1);
            sampleMap3.Add(_sample640_3, _sampleData2);
            sampleMap3.Add(_sample640_3, _sampleData3);

            IDictionary<Number640, Data> sampleMap4 = new Dictionary<Number640, Data>();
            sampleMap4.Add(_sample640_1, _sampleData1);
            sampleMap4.Add(_sample640_2, _sampleData2);
            sampleMap4.Add(_sample640_3, _sampleData3);

            IDictionary<Number640, Data> sampleMap5 = new Dictionary<Number640, Data>();
            sampleMap5.Add(_sample640_3, _sampleData1);
            sampleMap5.Add(_sample640_2, _sampleData2);
            sampleMap5.Add(_sample640_1, _sampleData3);

            var m1 = Utils2.CreateDummyMessage();
            m1.SetDataMap(new DataMap(sampleMap1));
            m1.SetDataMap(new DataMap(sampleMap2));
            m1.SetDataMap(new DataMap(sampleMap3));
            m1.SetDataMap(new DataMap(sampleMap4));
            m1.SetDataMap(new DataMap(sampleMap5));
            m1.SetDataMap(new DataMap(sampleMap1));
            m1.SetDataMap(new DataMap(sampleMap2));
            m1.SetDataMap(new DataMap(sampleMap3));

            // compare Java encoded and .NET decoded objects
            var m2 = DecodeMessage(JarRunner.RequestJavaBytes());

            Assert.IsTrue(CheckSameContentTypes(m1, m2));
            Assert.IsTrue(CheckIsSameList(m1.DataMapList, m2.DataMapList));
            
            // TODO implement Data to finish testing
            Assert.IsTrue(false);
        }

        [Test]
        public void TestMessageDecodeMapKey640Keys()
        {
            // create same message object as in Java
            var keysMap = new SortedDictionary<Number640, ICollection<Number160>>();
            var set = new HashSet<Number160>();
            set.Add(_sample160_1);
            keysMap.Add(_sample640_1, set);

            set = new HashSet<Number160>();
            set.Add(_sample160_2);
            set.Add(_sample160_3);
            keysMap.Add(_sample640_2, set);

            set = new HashSet<Number160>();
            set.Add(_sample160_1);
            set.Add(_sample160_2);
            set.Add(_sample160_3);
            set.Add(_sample160_4);
            set.Add(_sample160_5);
            keysMap.Add(_sample640_3, set);

            var m1 = Utils2.CreateDummyMessage();
            m1.SetKeyMap640Keys(new KeyMap640Keys(keysMap));
            m1.SetKeyMap640Keys(new KeyMap640Keys(keysMap));
            m1.SetKeyMap640Keys(new KeyMap640Keys(keysMap));
            m1.SetKeyMap640Keys(new KeyMap640Keys(keysMap));
            m1.SetKeyMap640Keys(new KeyMap640Keys(keysMap));
            m1.SetKeyMap640Keys(new KeyMap640Keys(keysMap));
            m1.SetKeyMap640Keys(new KeyMap640Keys(keysMap));
            m1.SetKeyMap640Keys(new KeyMap640Keys(keysMap));

            // compare Java encoded and .NET decoded objects
            var m2 = DecodeMessage(JarRunner.RequestJavaBytes());

            Assert.IsTrue(CheckSameContentTypes(m1, m2));
            Assert.IsTrue(CheckIsSameList(m1.KeyMap640KeysList, m2.KeyMap640KeysList));
        }

        [Test]
        public void TestMessageDecodeSetKey640()
        {
            // create same message object as in Java
            ICollection<Number160> sampleCollection1 = new List<Number160>();
            sampleCollection1.Add(_sample160_1);
            sampleCollection1.Add(_sample160_2);
            sampleCollection1.Add(_sample160_3);

            ICollection<Number160> sampleCollection2 = new List<Number160>();
            sampleCollection2.Add(_sample160_2);
            sampleCollection2.Add(_sample160_3);
            sampleCollection2.Add(_sample160_4);

            ICollection<Number160> sampleCollection3 = new List<Number160>();
            sampleCollection3.Add(_sample160_3);
            sampleCollection3.Add(_sample160_4);
            sampleCollection3.Add(_sample160_5);

            var m1 = Utils2.CreateDummyMessage();
            m1.SetKeyCollection(new KeyCollection(_sample160_1, _sample160_1, _sample160_1, sampleCollection1));
            m1.SetKeyCollection(new KeyCollection(_sample160_2, _sample160_2, _sample160_2, sampleCollection2));
            m1.SetKeyCollection(new KeyCollection(_sample160_3, _sample160_3, _sample160_3, sampleCollection3));
            m1.SetKeyCollection(new KeyCollection(_sample160_4, _sample160_4, _sample160_4, sampleCollection1));
            m1.SetKeyCollection(new KeyCollection(_sample160_5, _sample160_5, _sample160_5, sampleCollection2));
            m1.SetKeyCollection(new KeyCollection(_sample160_1, _sample160_2, _sample160_3, sampleCollection3));
            m1.SetKeyCollection(new KeyCollection(_sample160_2, _sample160_3, _sample160_4, sampleCollection1));
            m1.SetKeyCollection(new KeyCollection(_sample160_3, _sample160_4, _sample160_5, sampleCollection2));

            // read Java encoded bytes
            var m2 = DecodeMessage(JarRunner.RequestJavaBytes());

            Assert.IsTrue(CheckSameContentTypes(m1, m2));
            Assert.IsTrue(CheckIsSameList(m1.KeyCollectionList, m2.KeyCollectionList));
        }

        [Test]
        public void TestMessageDecodeSetNeighbors()
        {
            // create same message object as in Java
            var sampleAddress1 = new PeerAddress(_sample160_1, IPAddress.Parse("192.168.1.1"));
            var sampleAddress2 = new PeerAddress(_sample160_2, IPAddress.Parse("255.255.255.255"));
            var sampleAddress3 = new PeerAddress(_sample160_3, IPAddress.Parse("127.0.0.1"));
            var sampleAddress4 = new PeerAddress(_sample160_4, IPAddress.Parse("0:1:2:3:4:5:6:7"));
            var sampleAddress5 = new PeerAddress(_sample160_5, IPAddress.Parse("7:6:5:4:3:2:1:0"));

            ICollection<PeerAddress> sampleNeighbours1 = new List<PeerAddress>();
            sampleNeighbours1.Add(sampleAddress1);
            sampleNeighbours1.Add(sampleAddress2);
            sampleNeighbours1.Add(sampleAddress3);

            ICollection<PeerAddress> sampleNeighbours2 = new List<PeerAddress>();
            sampleNeighbours2.Add(sampleAddress2);
            sampleNeighbours2.Add(sampleAddress3);
            sampleNeighbours2.Add(sampleAddress4);

            ICollection<PeerAddress> sampleNeighbours3 = new List<PeerAddress>();
            sampleNeighbours3.Add(sampleAddress3);
            sampleNeighbours3.Add(sampleAddress4);
            sampleNeighbours3.Add(sampleAddress5);

            var m1 = Utils2.CreateDummyMessage();
            m1.SetNeighborSet(new NeighborSet(-1, sampleNeighbours1));
            m1.SetNeighborSet(new NeighborSet(-1, sampleNeighbours2));
            m1.SetNeighborSet(new NeighborSet(-1, sampleNeighbours3));
            m1.SetNeighborSet(new NeighborSet(-1, sampleNeighbours1));
            m1.SetNeighborSet(new NeighborSet(-1, sampleNeighbours2));
            m1.SetNeighborSet(new NeighborSet(-1, sampleNeighbours3));
            m1.SetNeighborSet(new NeighborSet(-1, sampleNeighbours1));
            m1.SetNeighborSet(new NeighborSet(-1, sampleNeighbours2));

            // compare Java encoded and .NET decoded objects
            var m2 = DecodeMessage(JarRunner.RequestJavaBytes());

            Assert.IsTrue(CheckSameContentTypes(m1, m2));
            Assert.IsTrue(CheckIsSameList(m1.NeighborSetList, m2.NeighborSetList));
        }

        [Test]
        public void TestMessageDecodeByteBuffer()
        {
            // create same message object as in Java
            var sampleBuf1 = new MemoryStream();
            sampleBuf1.WriteBytes(_sampleBytes1);
            sampleBuf1.WriteBytes(_sampleBytes1);
            sampleBuf1.WriteBytes(_sampleBytes1);

            /*var sampleBuf2 = new MemoryStream();
            sampleBuf2.WriteBytes(_sampleBytes2);
            sampleBuf2.WriteBytes(_sampleBytes2);
            sampleBuf2.WriteBytes(_sampleBytes2);

            var sampleBuf3 = new MemoryStream();
            sampleBuf3.WriteBytes(_sampleBytes3);
            sampleBuf3.WriteBytes(_sampleBytes3);
            sampleBuf3.WriteBytes(_sampleBytes3);

            var sampleBuf4 = new MemoryStream();
            sampleBuf4.WriteBytes(_sampleBytes1);
            sampleBuf4.WriteBytes(_sampleBytes2);
            sampleBuf4.WriteBytes(_sampleBytes3);*/

            var m1 = Utils2.CreateDummyMessage();
            m1.SetBuffer(new Buffer(sampleBuf1));
            /*m1.SetBuffer(new Buffer(sampleBuf2));
            m1.SetBuffer(new Buffer(sampleBuf3));
            m1.SetBuffer(new Buffer(sampleBuf4));
            m1.SetBuffer(new Buffer(sampleBuf1));
            m1.SetBuffer(new Buffer(sampleBuf2));
            m1.SetBuffer(new Buffer(sampleBuf3));
            m1.SetBuffer(new Buffer(sampleBuf4));*/

            // compare Java encoded and .NET decoded objects
            var m2 = DecodeMessage(JarRunner.RequestJavaBytes());

            Assert.IsTrue(CheckSameContentTypes(m1, m2));
            Assert.IsTrue(CheckIsSameList(m1.BufferList, m2.BufferList));

            // TODO implement DataBuffer and ByteBuffer decoding to finish testing
            Assert.IsTrue(false);
        }

        [Test]
        public void TestMessageDecodeLong()
        {
            // create same message object as in Java
            var m1 = Utils2.CreateDummyMessage();
            m1.SetLongValue(Int64.MinValue);
            m1.SetLongValue(-256);
            m1.SetLongValue(-128);
            m1.SetLongValue(-1);
            m1.SetLongValue(0);
            m1.SetLongValue(1);
            m1.SetLongValue(128);
            m1.SetLongValue(Int64.MaxValue);

            // compare Java encoded and .NET decoded objects
            var m2 = DecodeMessage(JarRunner.RequestJavaBytes());

            Assert.IsTrue(CheckSameContentTypes(m1, m2));
            Assert.IsTrue(CheckIsSameList(m1.LongList, m2.LongList));
        }

        [Test]
        public void TestMessageDecodeInteger()
        {
            // create same message object as in Java
            var m1 = Utils2.CreateDummyMessage();
		    m1.SetIntValue(Int32.MinValue);
		    m1.SetIntValue(-256);
		    m1.SetIntValue(-128);
		    m1.SetIntValue(-1);
		    m1.SetIntValue(0);
		    m1.SetIntValue(1);
		    m1.SetIntValue(128);
		    m1.SetIntValue(Int32.MaxValue);

            // compare Java encoded and .NET decoded objects
            var m2 = DecodeMessage(JarRunner.RequestJavaBytes());

            Assert.IsTrue(CheckSameContentTypes(m1, m2));
            Assert.IsTrue(CheckIsSameList(m1.IntList, m2.IntList));
        }

        [Test]
        public void TestMessageDecodePublicKeySignature()
        {
            // create same message object as in Java

            // TODO implement PrivateKeySignature decoding to finish testing
            Assert.IsTrue(false);
        }

        [Test]
        public void TestMessageDecodePublicKey()
        {
            // create same message object as in Java

            // TODO implement PrivateKey decoding to finish testing
            Assert.IsTrue(false);
        }

        [Test]
        public void TestMessageDecodeSetTrackerData()
        {
            // create same message object as in Java
            var sampleAddress1 = new PeerAddress(_sample160_1, IPAddress.Parse("192.168.1.1"));
            var sampleAddress2 = new PeerAddress(_sample160_2, IPAddress.Parse("255.255.255.255"));
            var sampleAddress3 = new PeerAddress(_sample160_3, IPAddress.Parse("127.0.0.1"));
            var sampleAddress4 = new PeerAddress(_sample160_4, IPAddress.Parse("0:1:2:3:4:5:6:7"));
            var sampleAddress5 = new PeerAddress(_sample160_5, IPAddress.Parse("7:6:5:4:3:2:1:0"));

            var sampleStatistic1 = new PeerStatistic(sampleAddress1);
            var sampleStatistic2 = new PeerStatistic(sampleAddress2);
            var sampleStatistic3 = new PeerStatistic(sampleAddress3);
            var sampleStatistic4 = new PeerStatistic(sampleAddress4);
            var sampleStatistic5 = new PeerStatistic(sampleAddress5);

            IDictionary<PeerStatistic, Data> sampleMap1 = new Dictionary<PeerStatistic, Data>();
            sampleMap1.Add(sampleStatistic1, _sampleData1);
            sampleMap1.Add(sampleStatistic2, _sampleData2);
            sampleMap1.Add(sampleStatistic3, _sampleData3);

            IDictionary<PeerStatistic, Data> sampleMap2 = new Dictionary<PeerStatistic, Data>();
            sampleMap2.Add(sampleStatistic2, _sampleData1);
            sampleMap2.Add(sampleStatistic3, _sampleData2);
            sampleMap2.Add(sampleStatistic4, _sampleData3);

            IDictionary<PeerStatistic, Data> sampleMap3 = new Dictionary<PeerStatistic, Data>();
            sampleMap3.Add(sampleStatistic3, _sampleData1);
            sampleMap3.Add(sampleStatistic4, _sampleData2);
            sampleMap3.Add(sampleStatistic5, _sampleData3);

            var m = Utils2.CreateDummyMessage();
            m.SetTrackerData(new TrackerData(sampleMap1, true));
            m.SetTrackerData(new TrackerData(sampleMap1, false));
            m.SetTrackerData(new TrackerData(sampleMap2, true));
            m.SetTrackerData(new TrackerData(sampleMap2, false));
            m.SetTrackerData(new TrackerData(sampleMap3, true));
            m.SetTrackerData(new TrackerData(sampleMap3, false));
            m.SetTrackerData(new TrackerData(sampleMap1, true));
            m.SetTrackerData(new TrackerData(sampleMap1, false));

            // implement Data and PeerStatistics first
            Assert.IsTrue(false);

            // TODO check how TrackerData should be compared (contains Map<PeerStatistic, Data>)
        }

        /*/// <summary>
        /// Checks if two messages are the same.
        /// </summary>
        /// <param name="m1">The first message.</param>
        /// <param name="m2">The second message.</param>
        private static void CompareMessages(Message.Message m1, Message.Message m2)
        {
            CheckSameContentTypes(m1, m2);
            
            Assert.AreEqual(m1.MessageId, m2.MessageId);
            Assert.AreEqual(m1.Version, m2.Version);
            Assert.AreEqual(m1.Command, m2.Command);
            Assert.AreEqual(m1.Recipient, m2.Recipient);
            Assert.AreEqual(m1.Type, m2.Type);
            Assert.AreEqual(m1.Sender, m2.Sender);
            Assert.AreEqual(m1.Sender.TcpPort, m2.Sender.TcpPort);
            Assert.AreEqual(m1.Sender.IsFirewalledTcp, m2.Sender.IsFirewalledTcp);
            Assert.AreEqual(m1.Sender.IsFirewalledUdp, m2.Sender.IsFirewalledUdp);

            Assert.IsTrue(Utils.Utils.IsSameSets(m1.BloomFilterList(), m2.BloomFilterList()));
            Assert.IsTrue(Utils.Utils.IsSameSets(m1.BufferList, m2.BufferList));
            Assert.IsTrue(Utils.Utils.IsSameSets(m1.IntList, m2.IntList));
            Assert.IsTrue(Utils.Utils.IsSameSets(m1.KeyList, m2.KeyList));
            Assert.IsTrue(Utils.Utils.IsSameSets(m1.KeyCollectionList, m2.KeyCollectionList));
            Assert.IsTrue(Utils.Utils.IsSameSets(m1.KeyMap640KeysList, m2.KeyMap640KeysList));
            Assert.IsTrue(Utils.Utils.IsSameSets(m1.LongList, m2.LongList));

            Assert.AreEqual(m1.DataMapList.Count(), m2.DataMapList.Count());
            Assert.AreEqual(m1.NeighborSetList.Count(), m2.NeighborSetList.Count());
            // TODO compare DataMapList contents
            // TODO compare NeighborSetList contents
        }*/

        /// <summary>
        /// Decodes a message from the provided byte array.
        /// </summary>
        /// <param name="bytes">The message bytes from Java encoding.</param>
        /// <returns>The .NET message version.</returns>
        private static Message.Message DecodeMessage(byte[] bytes)
        {
            var ms = new MemoryStream(bytes);
            var br = new JavaBinaryReader(ms);

            var decoder = new Decoder(null);

            // create dummy sender for decoding
            var message = Utils2.CreateDummyMessage();
            decoder.Decode(br, message.Recipient.CreateSocketTcp(), message.Sender.CreateSocketTcp());

            return decoder.Message;
        }

        /// <summary>
        /// Checks if two message's content types are the same.
        /// </summary>
        /// <param name="m1">The first message.</param>
        /// <param name="m2">The first message.</param>
        private static bool CheckSameContentTypes(Message.Message m1, Message.Message m2)
        {
            for (int i = 0; i < m1.ContentTypes.Length; i++)
            {
                var type1 = m1.ContentTypes[i];
                var type2 = m2.ContentTypes[i];

                if (!type1.Equals(type2))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CheckIsSameList<T>(IList<T> list1, IList<T> list2) 
        {
            if (list1 == null ^ list2 == null) // XOR
            {
                return false;
            }
            if (list1 != null && (list1.Count != list2.Count))
            {
                return false;
            }

            for (int i = 0; i < list1.Count; i++)
            {
                if (!list1[i].Equals(list2[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
