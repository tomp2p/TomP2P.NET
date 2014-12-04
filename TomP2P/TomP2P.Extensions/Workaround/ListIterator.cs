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
    public class ListIterator<T> where T : class
    {
        private readonly IList<T> _list; 
        private int _index;

        private T _previous = null;

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
            try
            {
                Previous();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                // Previous() decreased the index
                _index++;
            }
        }

        public T Previous()
        {
            return _list[_index-- - 1];
        }
        /// <summary>
        /// Removes from the list the last element that was returned by Previous.
        /// </summary>
        public void Remove()
        {
            // TODO check if works
            _list.Remove(_previous);
            _previous = null;
        }

        /// <summary>
        /// Replaces the last element returned by Previous with the specified element.
        /// </summary>
        /// <param name="t"></param>
        public void Set(T t)
        {
            // TODO check if works
            _list[_list.IndexOf(_previous)] = t;
        }
    }
}
