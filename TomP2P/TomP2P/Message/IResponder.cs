
namespace TomP2P.Message
{
    public interface IResponder
    {
        void Response(Message responseMessage);

        void Failed(Message.MessageType type, string reason);

        void ResponseFireAndForget(bool isUdp);
    }
}
