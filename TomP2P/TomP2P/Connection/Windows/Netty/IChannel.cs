using System.Net.Sockets;

namespace TomP2P.Connection.Windows.Netty
{
    public delegate void ClosedEventHandler(IChannel channel);
    
    /// <summary>
    /// Interface to expose Java Netty's Channel API that is needed for this project.
    /// </summary>
    public interface IChannel
    {
        event ClosedEventHandler Closed;

        void Close();

        /// <summary>
        /// The underlying socket that is used.
        /// </summary>
        Socket Socket { get; }

        Pipeline Pipeline { get; }

        bool IsUdp { get; }

        bool IsTcp { get; }

        bool IsOpen { get; }
    }

    /// <summary>
    /// Interface for all TCP channels.
    /// </summary>
    public interface ITcpChannel : IChannel
    {
        bool IsActive { get; }
    }

    /// <summary>
    /// Interface for all UDP channels.
    /// </summary>
    public interface IUdpChannel : IChannel
    { }
}
