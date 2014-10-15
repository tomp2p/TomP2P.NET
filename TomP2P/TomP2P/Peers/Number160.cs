using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

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

        public const int IntArraySize = Bits/32; // Integer.SIZE = 32

        // backing integer array
        private readonly int[] _val;

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

        public int CompareTo(Number160 other)
        {
            throw new NotImplementedException();
        }
    }
}
