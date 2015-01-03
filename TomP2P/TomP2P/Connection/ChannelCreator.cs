using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private readonly ISet<UdpClientSocket> _recipients = new HashSet<UdpClientSocket>();

        private readonly int _maxPermitsUdp;
        private readonly int _maxPermitsTcp;

        private readonly Semaphore _semaphoreUdp;
        private readonly Semaphore _semaphoreTcp;

        // we should be fair, otherwise we see connection timeouts due to unfairness if busy
        private readonly ReaderWriterLockSlim _readWriteLockTcp = new ReaderWriterLockSlim(); // TODO correct equivalent?
        private readonly ReaderWriterLockSlim _readWriteLockUdp = new ReaderWriterLockSlim(); // TODO correct equivalent?

        private readonly Bindings _externalBindings;

        private bool _shutdownUdp = false;
        private bool _shutdownTcp = false;

        /// <summary>
        /// Internal constructor since this is created by <see cref="Reservation"/> and should never be called directly.
        /// </summary>
        internal ChannelCreator(ChannelClientConfiguration channelClientConfiguration)
        {
            // TODO implement

            _externalBindings = channelClientConfiguration.BindingsOutgoing();

            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a "channel" to the given address.
        /// This won't send any message unlike TCP.
        /// </summary>
        /// <param name="broadcast">Sets this channel to be able to broadcast.</param>
        /// <param name="futureResponse"></param>
        public Task CreateUdp(bool broadcast, FutureResponse futureResponse)
        {
            // TODO limit/lock resources as in Java
            _readWriteLockUdp.EnterReadLock();
            try
            {
                if (_shutdownUdp)
                {
                    return null;
                }
                _semaphoreUdp.WaitOne(); // TODO blocks infinitely

                // TODO use correct local EP
                var localEndPoint = new IPEndPoint(IPAddress.Any, 5051);
                var udpSocket = new UdpClientSocket(localEndPoint);

                // TODO set broadcast option
                udpSocket.Bind(_ex);

                _recipients.Add(null);

                _semaphoreUdp.Release();

                return null; // TODO correct return type
            }
            finally
            {
                _readWriteLockUdp.ExitReadLock();
            }
        }
    }
}
