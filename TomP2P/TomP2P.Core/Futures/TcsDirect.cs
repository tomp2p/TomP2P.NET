using System.Threading.Tasks;
using TomP2P.Core.Message;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Core.Futures
{
    public class TcsDirect : BaseTcsImpl
    {
        public TaskCompletionSource<Message.Message> WrappedTcsResponse { get; private set; }

        public TcsDirect(string failed)
        {
            WrappedTcsResponse = new TaskCompletionSource<Message.Message>();
            WrappedTcsResponse.SetException(new TaskFailedException(failed));
        }

        public TcsDirect(TaskCompletionSource<Message.Message> tcsResponse)
        {
            WrappedTcsResponse = tcsResponse;
            WaitFor();
        }

        /// <summary>
        /// Wait for the future, which will cause this future to complete if 
        /// the wrapped future completes.
        /// </summary>
        private void WaitFor()
        {
            if (WrappedTcsResponse == null)
            {
                return;
            }
            WrappedTcsResponse.Task.ContinueWith(t =>
            {
                lock (Lock)
                {
                    if (!CompletedAndNotify())
                    {
                        return;
                    }
                    // TODO type & reason needed?
                }
                NotifyListeners();
            });
        }

        public Buffer Buffer
        {
            get
            {
                lock (Lock)
                {
                    return WrappedTcsResponse.Task.Result.Buffer(0);
                }
            }
        }

        public object Object
        {
            get
            {
                lock (Lock)
                {
                    // TODO no deadlock because of double-lock? (lock == this)
                    return Buffer.Object();
                }
            }
        }
    }
}
