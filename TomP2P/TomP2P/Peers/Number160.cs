using System;
using System.Text;

namespace TomP2P.Peers
{
    /// <summary>
    /// This class represents a 160 bit number.
    /// </summary>
    public class Number160 : IComparable<Number160>, IEquatable<Number160>
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
        public const int ByteSize = 8; // Byte.SIZE = 8;
        public const int ByteArraySize = Bits/ByteSize; 

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

        /// <summary>
        /// XOR operation.
        /// </summary>
        /// <param name="key">The second operand for the XOR operation.</param>
        /// <returns>A new key with the result of the XOR operation.</returns>
        public Number160 Xor(Number160 key)
        {
            var result = new int[IntArraySize];
            for (int i = 0; i < IntArraySize; i++)
            {
                result[i] = _val[i] ^ key._val[i];
            }
            return new Number160(result);
        }

        /// <summary>
        /// Returns a copy of the backing array, which is always of size 5.
        /// </summary>
        /// <returns>A copy of the backing array.</returns>
        public int[] ToIntArray()
        {
            var result = new int[IntArraySize];
            for (int i = 0; i < IntArraySize; i++)
            {
                result[i] = _val[i];
            }
            return result;
        }

        /// <summary>
        /// Returns a byte array, which is always of size 20.
        /// </summary>
        /// <returns>A byte array.</returns>
        public byte[] ToByteArray()
        {
            var result = new byte[ByteArraySize];
            ToByteArray(result, 0); // TODO check if the result variable has been modified
            return result;
        }

        /// <summary>
        /// Fills the byte array with this number.
        /// </summary>
        /// <param name="me">The byte array.</param>
        /// <param name="offset">Where to start in the byte array.</param>
        /// <returns>The offset being read.</returns>
        public int ToByteArray(byte[] me, int offset)
        {
            // TODO check if references are updated
            if (offset + ByteArraySize > me.Length)
            {
                throw new SystemException("Array too small.");
            }
            for (int i = 0; i < IntArraySize; i++)
            {
                // multiply by 4
                int idx = offset + (i << 2);
                me[idx + 0] = (byte)(_val[i] >> 24);
                me[idx + 1] = (byte)(_val[i] >> 16);
                me[idx + 2] = (byte)(_val[i] >> 8);
                me[idx + 3] = (byte)(_val[i]);
            }
            return offset + ByteArraySize;
        }

        public double ToDouble()
        {
            double d = 0;
            for (int i = 0; i < IntArraySize; i++)
            {
                d *= LongMask + 1;
                d += _val[i] & LongMask;
            }
            return d;
        }

        public float ToFloat()
        {
            return (float) ToDouble();
        }

        public int ToInt()
        {
            return _val[IntArraySize - 1];
        }

        public long ToLong()
        {
            return ((_val[IntArraySize - 1] & LongMask) << IntegerSize) + (_val[IntArraySize - 2] & LongMask);
        }

        /// <summary>
        /// Shows the content in a human-readable manner.
        /// </summary>
        /// <param name="removeLeadingZero">Indicates if leading zeros should be removed.</param>
        /// <returns>A human-readable representation of this key.</returns>
        public string ToString(bool removeLeadingZero)
        {
            bool removeZero = removeLeadingZero;
            var sb = new StringBuilder("0x");
            for (int i = 0; i < IntArraySize; i++)
            {
                ToHex(_val[i], removeZero, sb); // TODO check if reference works
                if (removeZero && _val[i] != 0)
                {
                    removeZero = false;
                }
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            return ToString(true);
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(obj, null))
            {
                return false;
            }
            if (Object.ReferenceEquals(this, obj))
            {
                return true;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            return this.Equals(obj as Number160);

        }

        public bool Equals(Number160 other)
        {
            for (int i = 0; i < IntArraySize; i++)
            {
                if (other._val[i] != _val[i])
                {
                    return false;
                }
            }
            return true;
        }

        // TODO check correct implementation (often used as hashtable-keys)
        public override int GetHashCode()
        {
            int hashCode = 0;
            for (int i = 0; i < IntArraySize; i++)
            {
                hashCode = (int) (31 * hashCode + (_val[i] & LongMask));
            }
            return hashCode;
        }

        public int CompareTo(Number160 other)
        {
            for (int i = 0; i < IntArraySize; i++)
            {
                long b1 = _val[i] & LongMask;
                long b2 = other._val[i] & LongMask;
                if (b1 < b2)
                {
                    return -1;
                }
                if (b1 > b2)
                {
                    return 1;
                }
            }
            return 0;
        }

        /// <summary>
        /// Creates a new Number160 from the integer, which fills all the 160 bits.
        /// A new random object will be created, thus, its thread-safe.
        /// </summary>
        /// <param name="intValue">The value to hash from. (Seed)</param>
        /// <returns>A hash based on pseudo radnom, to fill the 160 bits.</returns>
        public static Number160 CreateHash(int intValue)
        {
            return new Number160(new Random(intValue));
        }

        /// <summary>
        /// Creates a new Number160 using SHA-1 on the string.
        /// </summary>
        /// <param name="stringValue">The value to hash from.</param>
        /// <returns>A hash based on SHA-1 of the string.</returns>
        public static Number160 CreateHash(string stringValue)
        {
            // TODO compare result with java implementation
            return new Number160(stringValue.ComputeHash());
        }

        /// <summary>
        /// The first (most significant) 64 bits.
        /// </summary>
        public long Timestamp
        {
            get { return ((_val[0] & LongMask) << IntegerSize) + (_val[1] & LongMask); }
        }

        /// <summary>
        /// The lower (least significant) 96 bits.
        /// </summary>
        public Number160 Number96
        {
            get { return new Number160(0, 0, _val[2], _val[3], _val[4]); }
        }

        /// <summary>
        /// Check if this number is zero.
        /// </summary>
        public bool IsZero
        {
            get
            {
                for (int i = 0; i < IntArraySize; i++)
                {
                    if (_val[i] != 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// The number of bits used to represent this number. All leading (leftmost) zero bits are ignored.
        /// </summary>
        public int BitLength
        {
            get
            {
                int bits = 0;
                for (int i = 0; i < IntArraySize; i++)
                {
                    if (_val[i] != 0)
                    {
                        bits += (IntegerSize - _val[i].LeadingZeros()); // TODO test
                        bits += (IntegerSize * (IntArraySize - ++i));
                        break;
                    }
                }
                return bits;
            }
        }

        /// <summary>
        /// Convert an integer to a hexadecimal value.
        /// </summary>
        /// <param name="integer2">The integer to convert.</param>
        /// <param name="removeLeadingZero">Indicate if leading zeros should be ignored.</param>
        /// <param name="sb">The string builder where to store the result.</param>
        private static void ToHex(int integer2, bool removeLeadingZero, StringBuilder sb)
        {
            // 4 bits form a char, thus we have 160/4=40 chars in a key.
            // With an integer array size of 5, this gives 8 chars per integer.

            var buf = new char[CharsPerInt];
            int charPos = CharsPerInt;
            int integer = integer2;
            for (int i = 0; i < CharsPerInt && !(removeLeadingZero && integer == 0); i++)
            {
                buf[--charPos] = Digits[integer & CharMask];

                // for hexadecimal, we have 4 bits per char, which ranges from [0-9a-f]
                integer >>= 4; // TODO check if zero filling is done
            }
            sb.Append(buf, charPos, (CharsPerInt - charPos));
        }
    }
}