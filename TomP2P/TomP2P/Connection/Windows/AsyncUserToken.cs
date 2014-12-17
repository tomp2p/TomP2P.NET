using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows
{
    /// <summary>
    /// Token for use with SocketAsyncEventArgs.
    /// </summary>
    public sealed class AsyncUserToken : IDisposable
    {
        private Socket _connection;
        private byte[] _buffer;

        public AsyncUserToken(Socket connection, int bufferSize)
        {
            _connection = connection;
            _buffer = new byte[bufferSize];
        }

        /// <summary>
        /// Processes data received from the client.
        /// </summary>
        /// <param name="args"></param>
        public void ProcessData(SocketAsyncEventArgs args)
        {
            
        }

        /// <summary>
        /// Set data received from the client.
        /// </summary>
        /// <param name="args"></param>
        public void SetData(SocketAsyncEventArgs args)
        {
            
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
