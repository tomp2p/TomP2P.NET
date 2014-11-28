using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;
using TomP2P.Peers;

namespace TomP2P.Rpc
{
    // TODO maybe there is a .NET bloomfilter that can be used
    // TODO finish SimpleBloomFilter implementation (and documentation)

    /// <summary>
    /// A simple bloom filter that uses Random as a primitive hash function.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleBloomFilter<T> : IEquatable<SimpleBloomFilter<T>>
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();
        
        private const int SizeHeaderLength = 2;
        private const int SizeHeaderElements = 4;
        private const int SizeHeader = SizeHeaderLength + SizeHeaderElements;

        private readonly int _k;

        /// <summary>
        /// The bit array that backs the bloom filter.
        /// </summary>
        public BitArray BitArray { get; private set; }

        private readonly int _byteArraySize;
        private readonly int _bitArraySize;

        /// <summary>
        /// Constructs an empty SimpleBloomFilter. You must specify the number of bits in the bloom filter and also specify the number of items being expected to be added.
        /// The latter is used to choose some optimal internal values to minimize the false-positive rate. This can be expected with the ExpectedFalsePositiveRate property.
        /// </summary>
        /// <param name="byteArraySize">The number of bits in multiple of 8 in the bit array.(Often called 'm' in the context of bloom filters.)</param>
        /// <param name="expectedElements">The typical number of items expected to be added. (Often called 'n' in the context of bloom filters.)</param>
        public SimpleBloomFilter(int byteArraySize, int expectedElements)
            : this(byteArraySize, expectedElements, new BitArray(byteArraySize * 8))
        { }

        /// <summary>
        /// Constructs a SimpleBloomFilter out of existing data. You must specify the number of bits in the bloom filter and also specify the number of items being expected to be added.
        /// The latter is used to choose some optimal internal values to minimize the false-positive rate. This can be expected with the ExpectedFalsePositiveRate property.
        /// </summary>
        /// <param name="byteArraySize">The number of bits in multiple of 8 in the bit array.(Often called 'm' in the context of bloom filters.)</param>
        /// <param name="expectedElements">The typical number of items expected to be added. (Often called 'n' in the context of bloom filters.)</param>
        /// <param name="bitArray">The data to be used in the backing BitArray.</param>
        public SimpleBloomFilter(int byteArraySize, int expectedElements, BitArray bitArray)
        {
            _byteArraySize = byteArraySize;
            _bitArraySize = byteArraySize * 8;
            ExpectedElements = expectedElements;
            BitArray = bitArray;

            double hf = (_bitArraySize/(double) ExpectedElements)*Math.Log(2.0);
            _k = (int)Math.Ceiling(hf);

            if (hf < 1.0)
            {
                Logger.Warn("Bit size too small for storing all expected elements. For optimum result increase byte array size to {0}", ExpectedElements / Math.Log(2.0));
            }
        }

        public SimpleBloomFilter(double falsePositiveProbability, int expectedElements)
        {
            double c = Math.Ceiling(-(Math.Log(falsePositiveProbability) / Math.Log(2.0))) / Math.Log(2.0);
            var tmpBitArraySize = (int) Math.Ceiling(c*expectedElements);

            _byteArraySize = ((tmpBitArraySize + 7)/8);
            _bitArraySize = _byteArraySize*8;
            ExpectedElements = expectedElements;
            BitArray = new BitArray(_bitArraySize);

            double hf = (_bitArraySize/(double) ExpectedElements)*Math.Log(2.0);
            _k = (int) Math.Ceiling(hf);
        }

        /// <summary>
        /// Constructs a SimpleBloomFilter out of existing data.
        /// </summary>
        /// <param name="buffer">The byte buffer with the data.</param>
        public SimpleBloomFilter(JavaBinaryReader buffer)
        {
            _byteArraySize = buffer.ReadUShort() - (SizeHeaderElements - SizeHeaderLength);
            _bitArraySize = _byteArraySize*8;

            int expectedElements = buffer.ReadInt();
            ExpectedElements = expectedElements;
            double hf = (_bitArraySize / (double) expectedElements)*Math.Log(2.0);
            _k = (int) Math.Ceiling(hf);
            if (_byteArraySize > 0)
            {
                var me = new sbyte[_byteArraySize];
                buffer.ReadBytes(me);
                BitArray = new BitArray((byte[])(Array) me); // TODO test if OK to pass casted byte
            }
            else
            {
                BitArray = new BitArray(0); // TODO check if size needs/can extend later
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Always returns false.</returns>
        public bool Add(T item)
        {
            var r = new InteropRandom((ulong)item.GetHashCode());
            for (int x = 0; x < _k; x++)
            {
                BitArray.Set(r.NextInt(_bitArraySize), true);
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <param name="collection"></param>
        /// <returns>Always returns false.</returns>
        public bool AddAll<E>(IEnumerable<E> collection) where E : T // TODO correct use of generics?
        {
            foreach (var e in collection)
            {
                Add(e);
            }
            return false;
        }

        /// <summary>
        /// Clears this bloom filter.
        /// </summary>
        public void Clear()
        {
            for (int x = 0; x < BitArray.Length; x++)
            {
                BitArray.Set(x, false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>False indicates that o was definitely not added to this BloomFilter, true indicates that it probably was. The probability can be estimated using the expectedFalsePositiveProbability() method.</returns>
        public bool Contains(Object obj)
        {
            var r = new InteropRandom((ulong)obj.GetHashCode());
            for (int x = 0; x < _k; x++)
            {
                if (!BitArray.Get(r.NextInt(_bitArraySize)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        /// <returns>True, if all elements of the collection are in this bloom filter.</returns>
        public bool ContainsAll(IEnumerable collection)
        {
            foreach (var obj in collection)
            {
                if (!Contains(obj))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Converts data to a byte buffer. The first two bytes contain the size of this simple bloom filter. Thus, the bloom filter can only be of length 65536.
        /// </summary>
        /// <param name="buffer"></param>
        public void ToByteBuffer(JavaBinaryWriter buffer)
        {
            sbyte[] tmp = BitArray.ToByteArray();
            int currentByteArraySize = tmp.Length;
            buffer.WriteShort((short)(_byteArraySize + SizeHeader));
            buffer.WriteInt(ExpectedElements);
            buffer.WriteBytes(tmp);
            buffer.WriteZero(_byteArraySize - currentByteArraySize);
        }

        /// <summary>
        /// Merges this bloom filter with the provided one using OR.
        /// </summary>
        /// <param name="toMerge"></param>
        /// <returns>A new bloom filter that contains both sets.</returns>
        public SimpleBloomFilter<T> Merge(SimpleBloomFilter<T> toMerge)
        {
            if (toMerge._bitArraySize != _bitArraySize)
            {
                throw new SystemException("The two bloomfilters must have the same size.");
            }
            var mergedBitArray = (BitArray) BitArray.Clone();
            mergedBitArray.Or(toMerge.BitArray);
            return new SimpleBloomFilter<T>(_bitArraySize, ExpectedElements, mergedBitArray);
        }

        /// <summary>
        /// Inverts the bloomfilter.
        /// </summary>
        /// <returns></returns>
        public SimpleBloomFilter<Number160> Not()
        {
            var copy = (BitArray) BitArray.Clone();
            copy.Flip(0, copy.Length);
            return new SimpleBloomFilter<Number160>(_byteArraySize, ExpectedElements, copy);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as SimpleBloomFilter<T>);
        }

        public bool Equals(SimpleBloomFilter<T> other)
        {
            return _k == other._k 
                && _bitArraySize == other._bitArraySize
                && ExpectedElements == other.ExpectedElements
                && BitArray.Equals(other.BitArray);
        }

        public override int GetHashCode()
        {
            const int magic = 31;
            int hash = 7;
            hash = magic*hash + BitArray.GetHashCode();
            hash = magic*hash + _k;
            hash = magic*hash + ExpectedElements;
            hash = magic*hash + _bitArraySize;
            return hash;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            int length = BitArray.Length;
            for (int i = 0; i < length; i++)
            {
                sb.Append(BitArray.Get(i) ? "1" : "0");
            }
            return sb.ToString();
        }

        /// <summary>
        /// The expected elements that was provided.
        /// </summary>
        public int ExpectedElements { get; private set; }

        /// <summary>
        /// The approximate probability of the contains() method returning true for an object that had not 
        /// previously been inserted into the bloom filter. This is known as the "false positive probability".
        /// </summary>
        public double ExpectedFalsePositiveProbability
        {
            get { return Math.Pow((1 - Math.Exp(-_k*(double) ExpectedElements/_bitArraySize)), _k); }
        }
    }
}
