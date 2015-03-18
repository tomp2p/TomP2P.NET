using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Targets;
using TomP2P.Core.Connection.Windows;
using TomP2P.Core.Connection.Windows.Netty;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Core.Connection
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
        private readonly ISet<IChannel> _recipients = new HashSet<IChannel>();

        private readonly int _maxPermitsUdp;
        private readonly int _maxPermitsTcp;

        private readonly Semaphore _semaphoreUdp;
        private readonly Semaphore _semaphoreTcp;

        // we should be fair, otherwise we see connection timeouts due to unfairness if busy
        private readonly ReaderWriterLockSlim _readWriteLockTcp = new ReaderWriterLockSlim();
        private readonly ReaderWriterLockSlim _readWriteLockUdp = new ReaderWriterLockSlim();

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
            if (maxPermitsUdp > 0)
            {
                _semaphoreUdp = new Semaphore(maxPermitsUdp, maxPermitsUdp);
            }
            if (maxPermitsTcp > 0)
            {
                _semaphoreTcp = new Semaphore(maxPermitsTcp, maxPermitsTcp);
            }
        }

        /// <summary>
        /// Creates a "channel" to the given address.
        /// This won't send any message unlike TCP.
        /// </summary>
        /// <param name="broadcast">Sets this channel to be able to broadcast.</param>
        /// <param name="handlers">The handlers to be added to the channel's pipeline.</param>
        /// <returns>The created channel or null, if the channel could not be created.</returns>
        public MyUdpClient CreateUdp(bool broadcast, IDictionary<string, IChannelHandler> handlers)
        {
            _readWriteLockUdp.EnterReadLock();
            try
            {
                if (_shutdownUdp)
                {
                    return null;
                }
                // try to aquire resources for the channel
                if (_semaphoreUdp != null && !_semaphoreUdp.TryAcquire())
                {
                    const string errorMsg = "Tried to acquire more resources (UDP) than announced.";
                    Logger.Error(errorMsg);
                    throw new SystemException(errorMsg);
                }

                // create and bind
                var pipeline = new Pipeline(handlers);
                var filteredPipeline = _channelClientConfiguration.PipelineFilter.Filter(pipeline, false, true);

                var udpClient = new MyUdpClient(_externalBindings.WildcardSocket(), filteredPipeline);
                _recipients.Add(udpClient);
                SetupCloseListener(udpClient, _semaphoreUdp);

                if (broadcast)
                {
                    udpClient.Socket.EnableBroadcast = true;
                }

                return udpClient;
            }
            finally
            {
                _readWriteLockUdp.ExitReadLock();
            }
        }

        /// <summary>
        /// Creates a channel to the given address. This will setup the TCP connection.
        /// </summary>
        /// <param name="remoteAddress">The remote address.</param>
        /// <param name="connectionTimeoutMillis">The timeout for establishing a TCP connection.</param>
        /// <param name="handlers">The handlers to be added to the channel's pipeline.</param>
        /// <returns></returns>
        public MyTcpClient CreateTcp(IPEndPoint remoteAddress, int connectionTimeoutMillis, IDictionary<string, IChannelHandler> handlers)
        {
            _readWriteLockTcp.EnterReadLock();
            try
            {
                if (_shutdownTcp)
                {
                    return null;
                }
                // try to acquire resources for the channel
                if (_semaphoreTcp != null && !_semaphoreTcp.TryAcquire())
                {
                    const string errorMsg = "Tried to acquire more resources (TCP) than announced.";
                    Logger.Error(errorMsg);
                    throw new SystemException(errorMsg);
                }

                // create and bind
                var pipeline = new Pipeline(handlers);
                var filteredPipeline = _channelClientConfiguration.PipelineFilter.Filter(pipeline, true, true);

                var tcpClient = new MyTcpClient(_externalBindings.WildcardSocket(), filteredPipeline);
                _recipients.Add(tcpClient);
                SetupCloseListener(tcpClient, _semaphoreTcp);

                // TODO how to set CONNECT_TIMEOUT_MILLIS option?
                tcpClient.Socket.NoDelay = true;
                tcpClient.Socket.LingerState = new LingerOption(false, 0);
                tcpClient.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // connect
                try
                {
                    tcpClient.ConnectAsync(remoteAddress).Wait();
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                    throw;
                }

                return tcpClient;
            }
            finally
            {
                _readWriteLockTcp.ExitReadLock();
            }
        }

        /// <summary>
        /// When a channel is closed, the semaphore is released and another channel
        /// can be created. Also, the lock for the channel creating is being released.
        /// This means that the ChannelCreator can be shut down.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="semaphore">The semaphore to release.</param>
        private static void SetupCloseListener(IChannel channel, Semaphore semaphore)
        {
            // tcsResponse seems not to be needed here
            //Console.WriteLine("ChannelCreator: releasing semaphore for {0}.", channel);
            channel.Closed += ch => semaphore.Release(1);
            // TODO in Java, the FutureResponse is responded now after channel closing
        }

        /// <summary>
        /// Setup the close listener for a channel that was already created.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <param name="tcsResponse"></param>
        /// <returns>The channel that was passed as an argument.</returns>
        public IChannel SetupCloseListener(IChannel channel, TaskCompletionSource<Message.Message> tcsResponse)
        {
            // TODO in Java, the FutureResponse is responded now after channel closing
            return channel;
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
                    _tcsChannelShutdownDone.TrySetException(new TaskFailedException("Already shutting down."));
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

            // .NET-specific: close all channels
            // make async
            ThreadPool.QueueUserWorkItem(delegate
            {
                foreach (var client in _recipients)
                {
                    client.Close();
                }
                // we can block here
                if (_semaphoreUdp != null)
                {
                    //Console.WriteLine("ChannelCreator({0}): acquiring {1} UDP permits...", RuntimeHelpers.GetHashCode(this), _maxPermitsUdp);
                    _semaphoreUdp.Acquire(_maxPermitsUdp);
                }
                if (_semaphoreTcp != null)
                {
                    //Console.WriteLine("ChannelCreator: acquiring {0} TCP permits...", _maxPermitsTcp);
                    _semaphoreTcp.Acquire(_maxPermitsTcp);
                }
                _tcsChannelShutdownDone.SetResult(null); // complete
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
