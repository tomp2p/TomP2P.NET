using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// An attempt to mimick Java's AtomicReferenceArray in .NET.
    /// In .NET, however, it is reasonable to make it a struct rather than a class.
    /// </summary>
    public struct VolatileReferenceArray<T>
    {
        private readonly T[] _array;
        private object _lock;

        public VolatileReferenceArray(int size)
        {
            _array = new T[size];
            _lock = new object();
        }

        /// <summary>
        /// Creates a new VolatileReferenceArray with the same length as, 
        /// and all elements copied from, the given array.
        /// </summary>
        /// <param name="array"></param>
        public VolatileReferenceArray(T[] array)
        {
            _array = new T[array.Length];
            Array.Copy(array, _array, array.Length);
            _lock = new object();
        }

        public int Length
        {
            get { return _array.Length; }
        }

    }
}
