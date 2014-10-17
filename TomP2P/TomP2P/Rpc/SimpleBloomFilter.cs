using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Rpc
{
    public class SimpleBloomFilter<T> // TODO implement ISet<T>
    {
        // TODO serialzation ID required?

        private const int SizeHeaderLength = 2;
        private const int SizeHeaderElements = 4;
        private const int SizeHeader = SizeHeaderLength + SizeHeaderElements;

        private readonly int _k;
        private readonly BitArray _bitArray;
        private readonly int _byteArraySize;
        private readonly int _bitArraySize;
        private readonly int _expectedElements;
    }
}
