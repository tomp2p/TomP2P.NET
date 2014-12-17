using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace TomP2P.Connection.Windows
{
    // inspired by http://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.socketasynceventargs.aspx

    /// <summary>
    /// Represents a collection of reusable SocketAsyncEventArgs objects.
    /// </summary>
    public sealed class SocketAsyncEventArgsPool
    {
        private readonly Stack<SocketAsyncEventArgs> _pool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            _pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// <summary>
        /// Adds a SocketAsyncEventArgs object to the pool.
        /// </summary>
        /// <param name="item"></param>
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            lock (_pool)
            {
                _pool.Push(item);
            }
        }

        /// <summary>
        /// Removes and returns the SocketAsyncEventArgs object from the pool.
        /// Returns null if no more object is in the pool.
        /// </summary>
        /// <returns></returns>
        public SocketAsyncEventArgs Pop()
        {
            lock (_pool)
            {
                if (_pool.Count > 0)
                {
                    return _pool.Pop();
                }
                return null;
            }
        }

        public int Count
        {
            get { return _pool.Count; }
        }
    }
}
