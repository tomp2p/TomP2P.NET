using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Extensions.Workaround;
using TomP2P.Peers;

namespace TomP2P.Futures
{
    public class TcsPeerConnection : BaseTcsImpl
    {
        private PeerConnection _peerConnection;
        
        public PeerAddress RemotePeer { get; private set; }

        public TcsPeerConnection(PeerAddress remotePeer)
        {
            RemotePeer = remotePeer;
        }

        public PeerConnection PeerConnection
        {
            get
            {
                lock (Lock)
                {
                    return _peerConnection;
                }
            }
        }

        public TcsPeerConnection Done()
        {
            return Done(null);
        }

        public TcsPeerConnection Done(PeerConnection peerConnection)
        {
            lock (Lock)
            {
                if (!CompletedAndNotify())
                {
                    return this;
                }
                _peerConnection = peerConnection;
                // TODO type needed?
            }
            NotifyListeners();
            return this;
        }

        public Task CloseAsync()
        {
            var tcsShutdown = new TaskCompletionSource<object>();
            this.Task.ContinueWith(tpc =>
            {
                if (!tpc.IsFaulted)
                {
                    this.PeerConnection.CloseAsync().ContinueWith(t =>
                    {
                        if (!t.IsFaulted)
                        {
                            tcsShutdown.SetResult(null); // complete
                        }
                        else
                        {
                            tcsShutdown.SetException(new TaskFailedException("Could not close (1).", t));
                        }
                    });
                }
                else
                {
                    tcsShutdown.SetException(new TaskFailedException("Could not close (2).", tpc));
                }
            });
            return tcsShutdown.Task;
        }
    }
}
