using System;
using System.Net.Sockets;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Token for use with SocketAsyncEventArgs.
    /// </summary>
    public sealed class AsyncUserToken : IDisposable
    {
        private readonly Socket _connection;
        private readonly int _bufferSize;
        private byte[] _buffer;

        public AsyncUserToken(Socket connection, int bufferSize)
        {
            _connection = connection;
            _bufferSize = bufferSize;
            _buffer = new byte[_bufferSize];
        }

        /// <summary>
        /// Set data received from the client.
        /// </summary>
        /// <param name="args"></param>
        public void SetData(SocketAsyncEventArgs args)
        {
            var bytesRecv = args.BytesTransferred;

            if (bytesRecv > _bufferSize)
            {
                throw new ArgumentOutOfRangeException("args", "Buffer overflow on server side.");   
            }

            Array.Copy(args.Buffer, args.Offset, _buffer, 0, args.BytesTransferred);
        }

        /// <summary>
        /// Processes data received from the client.
        /// </summary>
        /// <param name="args"></param>
        public void ProcessData(SocketAsyncEventArgs args)
        {
            // echo
            var sendBuffer = new byte[_bufferSize];
            Array.Copy(_buffer, sendBuffer, _bufferSize);

            args.SetBuffer(sendBuffer, 0, _buffer.Length);
            //_buffer.Clear();
        }

        public void Dispose()
        {
            try
            {
                // TODO make UDP
                _connection.Shutdown(SocketShutdown.Send);
            }
            catch (Exception)
            {
            }
            finally
            {
                _connection.Close();
            }
        }

        public Socket Connection
        {
            get { return _connection; }
        }
    }
}
