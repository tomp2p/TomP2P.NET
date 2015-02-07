using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// An attempt to mimick Java's AtomicReferenceArray in .NET.
    /// </summary>
    public class VolatileReferenceArray<T>
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

        /// <summary>
        /// Gets the current value at the provided index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T Get(int index)
        {
            return _array[index];
        }

        /// <summary>
        /// Sets the element at the provided index to the given value.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void Set(int index, T value)
        {
            lock (_lock)
            {
                _array.SetValue(value, index);
            }
        }
    }
}
