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

        public SocketAsyncEventArgs Pop()
        {
            lock (_pool)
            {
                return _pool.Pop();
            }
        }

        public int Count
        {
            get { return _pool.Count; }
        }
    }
}
