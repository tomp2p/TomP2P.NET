using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace TomP2P.Rpc
{
    // TODO finish SimpleBloomFilter implementation (and documentation)

    /// <summary>
    /// A simple bloom filter that uses Random as a primitive hash function.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleBloomFilter<T> // TODO implement ISet<T>
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();
        
        // TODO serialzation ID required?

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
        /// The expected elements that was provided.
        /// </summary>
        public int ExpectedElements { get; private set; }

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
        /// Constructs a SimpleBloomFilter out of existing data. You must specify the number of bits in the bloom filter and also specify the number of items being expected to be added.
        /// The latter is used to choose some optimal internal values to minimize the false-positive rate. This can be expected with the ExpectedFalsePositiveRate property.
        /// </summary>
        public SimpleBloomFilter(MemoryStream channelBuffer)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        public void ToByteBuffer(MemoryStream buffer)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        public double ExpectedFalsePositiveProbability
        {
            get { return Math.Pow((1 - Math.Exp(-_k*(double) ExpectedElements/_bitArraySize)), _k); }
        }
    }
}
