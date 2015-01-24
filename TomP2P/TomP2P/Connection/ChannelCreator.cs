using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TomP2P.Connection.NET_Helper;
using TomP2P.Connection.Windows;
using TomP2P.Extensions;

namespace TomP2P.Connection
{
    /// <summary>
    /// Creates the channels. This class is created by <see cref="Reservation"/> and should never be called directly.
    /// With this class one can create TCP or UDP channels up to a certain extent. Thus it must be known beforehand
    /// how much creations will be created.
    /// </summary>
    public class ChannelCreator
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // the "ChannelGroup"
        private readonly ISet<MyUdpClient> _recipients = new HashSet<MyUdpClient>();

        private readonly int _maxPermitsUdp;
        private readonly int _maxPermitsTcp;

        private readonly Semaphore _semaphoreUdp;
        private readonly Semaphore _semaphoreTcp;

        // we should be fair, otherwise we see connection timeouts due to unfairness if busy
        private readonly ReaderWriterLockSlim _readWriteLockTcp = new ReaderWriterLockSlim(); // TODO correct equivalent?
        private readonly ReaderWriterLockSlim _readWriteLockUdp = new ReaderWriterLockSlim(); // TODO correct equivalent?

        private readonly TaskCompletionSource<object> _tcsChannelShutdownDone;

        private readonly ChannelClientConfiguration _channelClientConfiguration;
        private readonly Bindings _externalBindings;

        private bool _shutdownUdp = false;
        private bool _shutdownTcp = false;

        /// <summary>
        /// Internal constructor since this is created by <see cref="Reservation"/> and should never be called directly.
        /// </summary>
        /// <param name="tcsChannelShutdownDone"></param>
        /// <param name="maxPermitsUdp"></param>
        /// <param name="maxPermitsTcp"></param>
        /// <param name="channelClientConfiguration"></param>
        internal ChannelCreator(TaskCompletionSource<object> tcsChannelShutdownDone,
            int maxPermitsUdp, int maxPermitsTcp,
            ChannelClientConfiguration channelClientConfiguration)
        {
            _tcsChannelShutdownDone = tcsChannelShutdownDone;
            _maxPermitsUdp = maxPermitsUdp;
            _maxPermitsTcp = maxPermitsTcp;
            _channelClientConfiguration = channelClientConfiguration;
            _externalBindings = channelClientConfiguration.BindingsOutgoing;
            _semaphoreUdp = new Semaphore(maxPermitsUdp, maxPermitsUdp); // TODO correct?
            _semaphoreTcp = new Semaphore(maxPermitsUdp, maxPermitsTcp);
        }

        /// <summary>
        /// Creates a "channel" to the given address.
        /// This won't send any message unlike TCP.
        /// </summary>
        /// <param name="broadcast">Sets this channel to be able to broadcast.</param>
        /// <param name="senderEndPoint"></param>
        /// <returns>The created channel or null, if the channel could not be created.</returns>
        public MyUdpClient CreateUdp(bool broadcast, IPEndPoint senderEndPoint)
        {
            _readWriteLockUdp.EnterReadLock();
            try
            {
                if (_shutdownUdp)
                {
                    return null;
                }
                // try to aquire resources for the channel
                if (!_semaphoreUdp.WaitOne(TimeSpan.Zero))
                {
                    const string errorMsg = "Tried to acquire more resources (UDP) than announced.";
                    Logger.Error(errorMsg);
                    throw new SystemException(errorMsg);
                }

                // TODO surround with try/catch and return exception to TCS
                // TODO set broadcast option, etc.
                // create "channel", for which we use a socket in .NET
                //var udpSocket = new UdpClientSocket(senderEndPoint);
                //udpSocket.Bind(_externalBindings.WildcardSocket());

                var udpClient = new MyUdpClient(); // TODO bind to senderEp?

                _recipients.Add(udpClient);
                SetupCloseListener(udpClient, _semaphoreUdp);

                return udpClient;
            }
            finally
            {
                _readWriteLockUdp.ExitReadLock();
            }
        }

        public void CreateTcp()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// When a channel is closed, the semaphore is released and another channel
        /// can be created. Also, the lock for the channel creating is being released.
        /// This means that the ChannelCreator can be shut down.
        /// </summary>
        /// <param name="socket">The channel.</param>
        /// <param name="semaphore">The semaphore to release.</param>
        private void SetupCloseListener(MyUdpClient socket, Semaphore semaphore)
        {
            // TODO works?
            socket.Closed += sender =>
                {
                    semaphore.Release();
                };

            // TODO in Java, the FutureResponse is responded now after channel closing
        }

        /// <summary>
        /// Shuts down this channel creator. This means that no more TCP or UDP connections
        /// can be established.
        /// </summary>
        public Task ShutdownAsync()
        {
            // set shutdown flag for UDP and TCP
            // if we acquire a write lock, all read locks are blocked as well
            _readWriteLockUdp.EnterWriteLock();
            _readWriteLockTcp.EnterWriteLock();
            try
            {
                if (IsShutdown)
                {
                    _tcsChannelShutdownDone.SetException(new TaskFailedException("Already shutting down."));
                    return _tcsChannelShutdownDone.Task;
                }
                _shutdownUdp = true;
                _shutdownTcp = true;
            }
            finally
            {
                _readWriteLockUdp.ExitWriteLock();
                _readWriteLockTcp.ExitWriteLock();
            }

            // make async
            ThreadPool.QueueUserWorkItem(delegate
            {
                // .NET-specific to close all channels (Java has ChannelGroup.close())
                foreach (var client in _recipients) // TODO make also TCP
                {
                    client.Close();
                }
                // we can block here
                // TODO correct? workaround for multiple acquires/waitOnes in .NET
                _semaphoreUdp.Acquire(_maxPermitsUdp);
                _semaphoreTcp.Acquire(_maxPermitsTcp);
                _tcsChannelShutdownDone.SetResult(null); // completes the Task
            });

            return _tcsChannelShutdownDone.Task;
        }

        public override string ToString()
        {
            // available permits are not shown, as this is not a good practice
            var sb = new StringBuilder("ChannelCreator: addrUDP:").
                Append(_semaphoreUdp);
            return sb.ToString();
        }

        /// <summary>
        /// The shutdown task that is used when calling Shutdown().
        /// </summary>
        public Task ShutdownTask
        {
            get { return _tcsChannelShutdownDone.Task; }
        }

        public bool IsShutdown
        {
            get { return _shutdownTcp || _shutdownUdp; }
        }
    }
}
