using System.Net.Sockets;

namespace TomP2P.Connection.Windows.Netty
{
    public delegate void ClosedEventHandler(IChannel channel);
    
    /// <summary>
    /// Interface to expose Java Netty's Channel API that is needed for this project.
    /// </summary>
    public interface IChannel
    {
        // TODO add context

        event ClosedEventHandler Closed;

        void SetPipeline(Pipeline pipeline);

        /// <summary>
        /// Sends a message through the pipeline.
        /// </summary>
        /// <param name="message"></param>
        void Send(Message.Message message);

        void Close();

        /// <summary>
        /// The underlying socket that is used.
        /// </summary>
        Socket Socket { get; }

        Pipeline Pipeline { get; }

        bool IsUdp { get; }

        bool IsTcp { get; }
    }
}
