namespace TomP2P.Message
{
    public class NumberType
    {
        public NumberType(int number, Message.Content content)
        {
            Number = number;
            Content = content;
        }

        public int Number { get; private set; }
        public Message.Content Content { get; private set; }
    }
}
