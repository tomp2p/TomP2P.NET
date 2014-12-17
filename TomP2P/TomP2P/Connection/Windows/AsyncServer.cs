using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TomP2P.Connection.Windows
{
    // inspired by http://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs.aspx

    public class AsyncServer
    {
        private int _nrOfConnections;
        private int _clientCount;
        private int _bufferSize;
        private BufferManager _bufferManager;
        private const int OpsToPreAlloc = 2;    // read, write

        private Socket _serverSocket;
        private readonly SocketAsyncEventArgsPool _pool;
        private readonly Semaphore _semaphoreAcceptedClients;
        // TODO use Mutex to synchronize server execution?

        public AsyncServer(int nrOfConnections, int bufferSize)
        {
            _nrOfConnections = nrOfConnections;
            _bufferSize = bufferSize;
            _bufferManager = new BufferManager(_bufferSize * nrOfConnections * OpsToPreAlloc, _bufferSize);
            _pool = new SocketAsyncEventArgsPool(_nrOfConnections);
            _semaphoreAcceptedClients = new Semaphore(_nrOfConnections, _nrOfConnections);

            // pre-allocate pool of reusable SocketAsyncEventArgs
            for (int i = 0; i < _nrOfConnections; i++)
            {
                var rwArg = new SocketAsyncEventArgs();
                rwArg.Completed += IO_Completed;

                // assign a byte buffer from the buffer pool
                _bufferManager.AssignBuffer(rwArg);

                // add SocketAsyncEventArgs to the pool
                _pool.Push(rwArg);
            }
        }

        /// <summary>
        /// Starts the server such that it is listening for incoming connection requests.
        /// </summary>
        /// <param name="localEndPoint"></param>
        public void Start(IPEndPoint localEndPoint)
        {
            // TODO this is TCP only atm, support UDP too
            _serverSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // bind
            if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // set dual-mode (IPv4 & IPv6) for the socket listener
                // see http://blogs.msdn.com/wndp/archive/2006/10/24/creating-ip-agnostic-applications-part-2-dual-mode-sockets.aspx
                _serverSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                _serverSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, localEndPoint.Port));
            }
            else
            {
                _serverSocket.Bind(localEndPoint);
            }

            // listen
            if (_serverSocket.ProtocolType == ProtocolType.Tcp)
            {
                _serverSocket.Listen(10); // TODO find appropriate backlog
            }

            // accept
            StartAccept(null);

            // TODO use Mutex.WaitOne()?
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Stop()
        {
            _serverSocket.Close();
            // TODO use Mutex.ReleaseMutex()?
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            if (acceptEventArgs == null)
            {
                acceptEventArgs = new SocketAsyncEventArgs();
                acceptEventArgs.Completed += Accept_Completed;
            }
            else
            {
                // clear socket, since context object is being reused
                acceptEventArgs.AcceptSocket = null;
            }

            _semaphoreAcceptedClients.WaitOne();
            bool isPending = _serverSocket.AcceptAsync(acceptEventArgs);
            if (!isPending)
            {
                ProcessAccept(acceptEventArgs);
            }
        }

        /// <summary>
        /// Called whenever a receive or send operation is completed on a socket.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void IO_Completed(object sender, SocketAsyncEventArgs args)
        {
            // TODO this is TCP only atm, support UDP too
            switch (args.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(args);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(args);
                    break;
                default:
                    throw new ArgumentException("Unsupported completed last socket operation.");
            }
        }

        private void Accept_Completed(object sender, SocketAsyncEventArgs args)
        {
            // TODO why not merge with the others?
            ProcessAccept(args);
        }

        private void ProcessAccept(SocketAsyncEventArgs args)
        {
            var socket = args.AcceptSocket;
            if (socket.Connected)
            {
                try
                {
                    var readEventArgs = _pool.Pop();
                    if (readEventArgs != null)
                    {
                        // get the socket for the accepted client connection and put it
                        // into the user token
                        readEventArgs.UserToken = new AsyncUserToken(socket, _bufferSize);

                        Interlocked.Increment(ref _clientCount);

                        // as soon as the client is connected, post a receive to the connection
                        bool isPending = socket.ReceiveAsync(readEventArgs);
                        if (!isPending)
                        {
                            ProcessReceive(readEventArgs);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    // TODO Console.WriteLine("Error when processing data received from {0}:\r\n{1}", token.Connection.RemoteEndPoint, ex.ToString());
                    throw;
                }
                catch (Exception ex)
                {
                    throw;
                }

                // accept the next connection request
                StartAccept(args);
            }
        }

        /// <summary>
        /// Called when an async receive operation completes.
        /// If the remote host closed the connection, the socket is closed.
        /// If data was received, the data is echoed back to the client.
        /// </summary>
        /// <param name="args"></param>
        private void ProcessReceive(SocketAsyncEventArgs args)
        {
            // check if remote host closed the connection
            if (args.BytesTransferred > 0)
            {
                if (args.SocketError == SocketError.Success)
                {
                    var token = args.UserToken as AsyncUserToken;
                    token.SetData(args);

                    var socket = token.Connection;
                    if (socket.Available == 0)
                    {
                        // set return buffer
                        token.ProcessData(args);

                        // set the buffer to send back to the client
                        bool isPending = socket.SendAsync(args);
                        if (!isPending)
                        {
                            ProcessSend(args);
                        }
                    }
                    else if (!socket.ReceiveAsync(args))
                    {
                        // read the next block of data sent by the client
                        ProcessReceive(args);
                    }
                }
                else
                {
                    ProcessError(args);
                }
            }
            else
            {
                CloseClientSocket(args);
            }
        }

        /// <summary>
        /// Called when an async send operation completes.
        /// The method issues another receive on the socket to read any additional
        /// data sent from the client.
        /// </summary>
        /// <param name="args"></param>
        private void ProcessSend(SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                var token = args.UserToken as AsyncUserToken;

                if (!token.Connection.ReceiveAsync(args))
                {
                    // read the next block of data sent from the client
                    ProcessReceive(args);
                }
            }
            else
            {
                ProcessError(args);
            }
        }

        private void ProcessError(SocketAsyncEventArgs args)
        {
            // TODO log error
            //var token = args.UserToken as AsyncUserToken;
            //var localEp = token.Connection.LocalEndPoint as IPEndPoint;

            CloseClientSocket(args);
        }

        private void CloseClientSocket(SocketAsyncEventArgs args)
        {
            var token = args.UserToken as AsyncUserToken;

            // close the socket associated with the client
            if (token != null)
            {
                token.Dispose();
            }

            // decrement the counter keeping track of the total number of clients
            Interlocked.Decrement(ref _clientCount);
            _semaphoreAcceptedClients.Release();

            // free the SocketAsyncEventArgs so it can be reused
            _pool.Push(args);
        }
    }
}
