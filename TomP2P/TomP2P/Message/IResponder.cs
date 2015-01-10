
namespace TomP2P.Message
{
    public interface IResponder
    {
        // TODO shouldn't these methods return Message in .NET pipeline?
        void Response(Message responseMessage);

        void Failed(Message.MessageType type, string reason);

        void ResponseFireAndForget(bool isUdp);
    }
}
