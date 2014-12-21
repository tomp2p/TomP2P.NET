using System;
using System.Text;
using TomP2P.Peers;

namespace TomP2P.Message
{
    // TODO document according to updated JavaDoc
    public class MessageId : IComparable<MessageId>, IEquatable<MessageId>
    {
        // The message ID, which is, together with the peer address, unique.
        // However, we don't check this and cllisions will cause a message to fail.
        /// <summary>
        /// The message ID.
        /// </summary>
        public int Id { get; private set; }

        // The peer address depends on the message.
        // Either it is the sender or the recipient.
        /// <summary>
        /// The peer address of the sender or the recipient.
        /// </summary>
        public PeerAddress PeerAddress { get; private set; }

        /// <summary>
        /// Creates a message ID. If the message is a request, the peer address is the sender.
        /// Otherwise, it is the recipient. This is due to the fact that depending on the direction,
        /// peer addresses may change, but it is still considered the same message.
        /// </summary>
        /// <param name="message">The message.</param>
        public MessageId(Message message)
            : this(message.MessageId, message.IsRequest() ? message.Sender : message.Recipient)
        { }

        private MessageId(int id, PeerAddress peerAddress)
        {
            Id = id;
            PeerAddress = peerAddress;
        }

        public int CompareTo(MessageId other)
        {
            int diff = Id - other.Id;
            return diff == 0 ? PeerAddress.CompareTo(other.PeerAddress) : diff;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as MessageId);
        }

        public bool Equals(MessageId other)
        {
            return CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            return Id ^ PeerAddress.GetHashCode();
        }

        public override string ToString()
        {
            return new StringBuilder("MessageId: ")
                .Append(Id).Append("/")
                .Append(PeerAddress).ToString();
        }
    }
}
