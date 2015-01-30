
namespace TomP2P.Message
{
    public interface IResponder
    {
        void Response(Message responseMessage);

        void Failed(Message.MessageType type);

        void ResponseFireAndForget();
    }
}
