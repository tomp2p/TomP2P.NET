using System;
using System.Linq;
using System.Net.Sockets;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Token for use with SocketAsyncEventArgs.
    /// </summary>
    public sealed class AsyncUserToken : IDisposable
    {
        private readonly Socket _connection;
        private byte[] _buffer;

        public AsyncUserToken(Socket connection, int bufferSize)
        {
            _connection = connection;
            _buffer = new byte[bufferSize];
        }

        /// <summary>
        /// Set data received from the client.
        /// </summary>
        /// <param name="args"></param>
        public void SetData(SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > _buffer.Length)
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
            var sendBuffer = new byte[_buffer.Length];
            Array.Copy(_buffer, sendBuffer, _buffer.Length);

            args.SetBuffer(sendBuffer, 0, sendBuffer.Length);
            
            // clear buffer, so it can receive more data from a keep-alive connection client
            _buffer = new byte[_buffer.Length];
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
                // throw if client has closed, so it isn't necessary to catch
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
