using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Extensions.Netty
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
