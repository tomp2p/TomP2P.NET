using System.Net;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows.Netty
{
    public abstract class BaseClient : BaseChannel, IClientChannel
    {
        protected readonly Pipeline.PipelineSession Session;

        protected BaseClient(IPEndPoint localEndPoint)
            : base(localEndPoint)
        {
            Session = Pipeline.CreateClientSession();
        }

        public async Task SendMessageAsync(Message.Message message)
        {
            Session.TriggerActive();
            if (!Session.IsTimedOut)
            {
                // execute outbound pipeline
                var writeRes = Session.Write(message);
                Session.Reset();
                if (Session.IsTimedOut)
                {
                    return;
                }

                // send bytes
                var bytes = ConnectionHelper.ExtractBytes(writeRes);
                var senderEp = ConnectionHelper.ExtractSenderEp(message);
                var receiverEp = ConnectionHelper.ExtractReceiverEp(message);
                await SendBytesAsync(bytes, senderEp, receiverEp);
                NotifyWriteCompleted();
            }
            else
            {
                Close();
                Session.TriggerInactive();
            }
        }

        public async Task ReceiveMessageAsync()
        {
            if (!Session.IsTimedOut)
            {
                // receive bytes
                await DoReceiveMessageAsync();
            }
            else
            {
                Close();
            }
            Session.TriggerInactive();
        }

        public abstract Task SendBytesAsync(byte[] bytes, IPEndPoint senderEp, IPEndPoint receiverEp = null);

        public abstract Task DoReceiveMessageAsync();
    }
}
