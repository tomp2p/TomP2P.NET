using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TomP2P.Peers
{
    /// <summary>
    /// This class represents a 160 bit number.
    /// </summary>
    public class Number160 : IComparable<Number160>
    {
        // TODO serialVersionUID equivalent required?

        // This key has ALWAYS 160 bit. Do not change.
        public const int Bits = 160;

        public static readonly Number160 MaxValue = new Number160(new int[] {-1, -1, -1, -1, -1});

        private const long LongMask = 0xffffffffL;
        private const int ByteMask = 0xff;
        private const int CharMask = 0xf;

        private const int StringLength = 42; // TODO not 40? see string c'tor

        // map used for String <-> Key conversion
        private static readonly char[] Digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        // size of the backing integer array
        public const int IntegerSize = 32; // Integer.SIZE = 32
        public const int IntArraySize = Bits/IntegerSize; 

        // size of a byte array
        public const int ByteArraySize = Bits/8; // Byte.SIZE = 8;

        public const int CharsPerInt = 8;

        // backing integer array
        private readonly int[] _val;

        public static readonly Number160 Zero = new Number160(0);
        public static readonly Number160 One = new Number160(1);

        /// <summary>
        /// Create a key with value 0.
        /// </summary>
        public Number160()
        {
            _val = new int[IntArraySize];
        }

        /// <summary>
        /// Create an instance with an integer array. This integer array will be copied into the backing array.
        /// </summary>
        /// <param name="val">The value to copy to the backing array. Since this class stores 160 bit numbers, the array needs to be of size 5 or smaller.</param>
        public Number160(params int[] val)
        {
            if (val.Length > IntArraySize)
            {
                throw new InvalidOperationException(String.Format("Can only deal with arrays of size smaller or equal to {0}. Provided array has {1} length.", IntArraySize, val.Length));
            }
            _val = new int[IntArraySize];
            int len = val.Length;
            for (int i = len - 1, j = IntArraySize - 1; i >= 0; i--, j--)
            {
                _val[j] = val[i];
            }
        }

        /// <summary>
        /// Create an instance from a string. The string has to be of length 40 to fit into the backing array.
        /// Note that this string is ALWAYS in hexadecimal: There is no 0x... required before the number.
        /// </summary>
        /// <param name="val">The characters allowd are [0-9a-f] (hexadecimal).</param>
        public Number160(string val)
        {
            if (val.Length > StringLength)
            {
                throw new InvalidOperationException(String.Format("Can only deal with strings of size smaller or equal to {0}. Provided string has {1} length.", StringLength, val.Length));
            }
            if (val.IndexOf("0x", StringComparison.Ordinal) != 0) // TODO comparer needed? (cultural)
            {
                throw new InvalidOperationException(String.Format("{0} is not in a hexadecimal form. Decimal form is not supported yet.", val));
            }
            _val = new int[IntArraySize];
            char[] tmp = val.ToCharArray();
            int len = tmp.Length;
            for (int i = StringLength - len, j = 2; i < (StringLength - 2); i++, j++)
            {
                _val[i >> 3] <<= 4;

                int digit = Convert.ToInt32(tmp[j].ToString(), 16); // TODO check conversion
                if (digit < 0) // TODO what's returned in the conversion?
                {
                    throw new SystemException(String.Format("Not a hexadecimal number \"{0}\". The range is [0-9a-f].", tmp[j]));
                }
                _val[i >> 3] += digit & CharMask;
            }
        }

        /// <summary>
        /// Creates a key with the integer value.
        /// </summary>
        /// <param name="val">The integer value.</param>
        public Number160(int val)
        {
            _val = new int[IntArraySize];
            _val[IntArraySize - 1] = val;
        }

        /// <summary>
        /// Creates a key with the long value.
        /// </summary>
        /// <param name="val">Tge long value.</param>
        public Number160(long val)
        {
            _val = new int[IntArraySize];
            _val[IntArraySize - 1] = (int) val;
            _val[IntArraySize - 2] = (int) (val >> IntegerSize);
        }

        /// <summary>
        /// Creates a key with a byte array. The array is copied to the backing int[] array.
        /// </summary>
        /// <param name="val">The byte array.</param>
        public Number160(byte[] val) : this(val, 0, val.Length)
        {}

        /// <summary>
        /// Creates a key with the byte array. The array is copoed to the backing int[] array, starting at the given offset.
        /// </summary>
        /// <param name="val">The byte array.</param>
        /// <param name="offset">The offset where to start.</param>
        /// <param name="length">The length to read.</param>
        public Number160(byte[] val, int offset, int length)
        {
            if (length > ByteArraySize)
            {
                throw new InvalidOperationException(String.Format("Can only deal with byte arrays of size smaller or equal to {0}. Provided array has {1} length.", ByteArraySize, length));
            }
            _val = new int[IntArraySize];
            for (int i = length + offset - 1, j = ByteArraySize - 1, k = 0; i >= offset; i--, j--, k++)
            {
                // TODO test
                _val[j >> 2] |= (val[i] & ByteMask) << ((k%4) << 3);
            }
        }

        /// <summary>
        /// Creates a new key with random integer values.
        /// </summary>
        /// <param name="random">The object to create pseudo random numbers. For testing and debugging, the seed in the random class can be set to make the random values repeatable.</param>
        public Number160(Random random)
        {
            _val = new int[IntArraySize];
            for (int i = 0; i < IntArraySize; i++)
            {
                _val[i] = random.Next();
            }
        }

        /// <summary>
        /// Creates a new key with a long for the first 64 bits, and using the lower 96 bits for the rest.
        /// </summary>
        /// <param name="timestamp">The long value that will be set in the beginning (most significant) bit.</param>
        /// <param name="number96">The rest will be filled with this number.</param>
        public Number160(long timestamp, Number160 number96)
        {
            _val = new int[IntArraySize];
            _val[0] = (int)(timestamp >> IntegerSize);
            _val[1] = (int)timestamp;
            _val[2] = number96._val[2];
            _val[3] = number96._val[3];
            _val[4] = number96._val[4];
        }

        public int CompareTo(Number160 other)
        {
            throw new NotImplementedException();
        }
    }
}
