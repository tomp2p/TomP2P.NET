
namespace TomP2P.Connection
{
    public interface IResponder
    {
        void Response(Message.Message responseMessage);

        void Failed(Message.Message.MessageType type);

        void ResponseFireAndForget();
    }
}
