using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows
{
    public class AsyncServer
    {
        private int _nrOfConnections;
        private int _recvBufferSize;

        private Socket _serverSocket;
        private SocketAsyncEventArgsPool _pool;
        private Semaphore _maxNrAcceptedClients;

        public AsyncServer(int nrOfConnections, int recvBufferSize)
        {
            _nrOfConnections = nrOfConnections;
            _recvBufferSize = recvBufferSize;

            _pool = new SocketAsyncEventArgsPool(_nrOfConnections);
            _maxNrAcceptedClients = new Semaphore(_nrOfConnections, _nrOfConnections);
        }

        public void Init()
        {
            // pre-allocate pool of reusable SocketAsyncEventArgs
            SocketAsyncEventArgs rwArg;
            for (int i = 0; i < _nrOfConnections; i++)
            {
                rwArg = new SocketAsyncEventArgs();
                rwArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                rwArg.UserToken = new AsyncUserToken();

                // assign a byte buffer from the buffer pool


                // add SocketAsyncEventArgs to the pool
                _pool.Push(rwArg);
            }
        }
    }
}
