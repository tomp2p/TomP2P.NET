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
using TomP2P.Connection.Windows;
using TomP2P.Futures;

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

        private readonly ChannelClientConfiguration _channelClientConfiguration;
        private readonly Bindings _externalBindings;

        private bool _shutdownUdp = false;
        private bool _shutdownTcp = false;

        /// <summary>
        /// Internal constructor since this is created by <see cref="Reservation"/> and should never be called directly.
        /// </summary>
        internal ChannelCreator(int maxPermitsUdp, int maxPermitsTcp, ChannelClientConfiguration channelClientConfiguration)
        {
            _maxPermitsUdp = maxPermitsUdp; // TODO why not use value from ChannelClientConfig?
            _maxPermitsTcp = maxPermitsTcp;
            _semaphoreUdp = new Semaphore(0, maxPermitsUdp); // TODO correct?
            _semaphoreTcp = new Semaphore(0, maxPermitsTcp);
            _channelClientConfiguration = channelClientConfiguration;
            _externalBindings = channelClientConfiguration.BindingsOutgoing;
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
                if (!_semaphoreUdp.WaitOne(TimeSpan.FromMilliseconds(1))) // TODO blocks infinitely, use timeout
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

                var udpClient = new MyUdpClient(senderEndPoint); // binds to senderEp

                _recipients.Add(udpClient);
                SetupCloseListener(udpClient);

                return udpClient;
            }
            finally
            {
                _readWriteLockUdp.ExitReadLock();
            }
        }

        /// <summary>
        /// When a channel is closed, the semaphore is released and another channel
        /// can be created. Also, the lock for the channel creating is being released.
        /// This means that the ChannelCreator can be shut down.
        /// </summary>
        private void SetupCloseListener(MyUdpClient socket)
        {
            // TODO works?
            socket.Closed += sender => _semaphoreUdp.Release();

            // TODO in Java, the FutureResponse is responded now after channel closing
        }
    }
}
