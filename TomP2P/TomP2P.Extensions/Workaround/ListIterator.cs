using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// Equivalent of Java's ListIterator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListIterator<T>
    {
        private readonly IList<T> _list; 
        private int _index;

        /// <summary>
        /// Returns a list iterator over the elements in this list (in proper sequence), 
        /// starting at the specified position in the list. The specified index indicates 
        /// the first element that would be returned by an initial call to next. 
        /// An initial call to previous would return the element with the specified index 
        /// minus one.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="index"></param>
        public ListIterator(IList<T> list, int index)
        {
            _list = list;
            _index = index;
        }

        /// <summary>
        /// Returns true if this list iterator has more elements when traversing the list in the 
        /// reverse direction.  (In other words, returns true if Previous() would return an element
        /// rather than throwing an exception.)
        /// </summary>
        /// <returns></returns>
        public bool HasPrevious()
        {
            // TODO check if works
            try
            {
                Previous();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public T Previous()
        {
            // TODO check if works
            return _list[_index - 1];
        }
    }
}
