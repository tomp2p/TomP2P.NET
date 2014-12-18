using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows
{
    // inspired by http://msdn.microsoft.com/en-us/library/bb517542.aspx

    // TODO use .NET's BufferManager

    /// <summary>
    /// Single large buffer which can be divided up and assigned to SocketAsyncEventArgs objects
    /// for use with each socket IO operation.
    /// This allows buffers to be easily reused and guards against fragmenting heap memory.
    /// </summary>
    public class BufferManager
    {
        private int _nrOfBytes; // total number of bytes controlled by the buffer pool
        private byte[] _buffer;
        private Stack<int> _freeIndexPool;
        private int _currentIndex;
        private int _bufferSize;

        public BufferManager(int nrOfBytes, int bufferSize)
        {
            _nrOfBytes = nrOfBytes;
            _currentIndex = 0;
            _bufferSize = bufferSize;
            _freeIndexPool = new Stack<int>();

            _buffer = new byte[_nrOfBytes];
        }

        /// <summary>
        /// Assigns a buffer from the buffer pool to the specified SocketAsyncEventArgs object.
        /// </summary>
        /// <returns>True, if assignment was successful. False otherwise.</returns>
        public bool AssignBuffer(SocketAsyncEventArgs args)
        {
            if (_freeIndexPool.Count > 0)
            {
                args.SetBuffer(_buffer, _freeIndexPool.Pop(), _bufferSize);
            }
            else
            {
                if (_nrOfBytes - _bufferSize < _currentIndex)
                {
                    return false;
                }
                args.SetBuffer(_buffer, _currentIndex, _bufferSize);
                _currentIndex += _bufferSize;
            }
            return true;
        }

        /// <summary>
        /// Removes the buffer from a SocketAsyncEventArgs object. This frees the buffer back
        /// to the buffer pool.
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            _freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
