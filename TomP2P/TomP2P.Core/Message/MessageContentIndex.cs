namespace TomP2P.Core.Message
{
    /// <summary>
    /// Describes the index of a <see cref="Message.Content"/> enum in a <see cref="Message"/>.
    /// <para>Note: Each <see cref="Message"/> can contain up to 8 contents, so indices range from 0 to 7.</para>
    /// </summary>
    public class MessageContentIndex
    {
        /// <summary>
        /// The index of the associated content.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// The content of the associated index.
        /// </summary>
        public Message.Content Content { get; private set; }
        
        public MessageContentIndex(int index, Message.Content content)
        {
            Index = index;
            Content = content;
        }
    }
}
