
namespace TomP2P.Connection
{
    /// <summary>
    /// The class that stores the limits for the resource reservation.
    /// </summary>
    public class ChannelClientConfiguration
    {
        /// <summary>
        /// The maximum number of permanent (long-lived) connections.
        /// </summary>
        public int MaxPermitsPermanentTcp { get; private set; }
        /// <summary>
        /// The maximum number of short-lived UDP connections.
        /// </summary>
        public int MaxPermitsUdp { get; private set; }
        /// <summary>
        /// The maximum number of short-lived TCP connections.
        /// </summary>
        public int MaxPermitsTcp { get; private set; }

        // TODO pipelinefilter needed? (netty)

        /// <summary>
        /// The factory for the signature.
        /// </summary>
        public ISignatureFactory SignatureFactory { get; private set; }

        public Bindings BindingsOutgoing { get; private set; }

        /// <summary>
        /// Sets the maximum number of permanent (long-lived) connections.
        /// </summary>
        /// <param name="maxPermitsPermanentTcp"></param>
        /// <returns>This class.</returns>
        public ChannelClientConfiguration SetMaxPermitsPermanentTcp(int maxPermitsPermanentTcp)
        {
            MaxPermitsPermanentTcp = maxPermitsPermanentTcp;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of short-lived UDP connections.
        /// </summary>
        /// <param name="maxPermitsUdp"></param>
        /// <returns>This class.</returns>
        public ChannelClientConfiguration SetMaxPermitsUdp(int maxPermitsUdp)
        {
            MaxPermitsUdp = maxPermitsUdp;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of short-lived TCP connections.
        /// </summary>
        /// <param name="maxPermitsTcp"></param>
        /// <returns>This class.</returns>
        public ChannelClientConfiguration SetMaxPermitsTcp(int maxPermitsTcp)
        {
            MaxPermitsTcp = maxPermitsTcp;
            return this;
        }

        /// <summary>
        /// Sets the factory for the signature.
        /// </summary>
        /// <param name="signatureFactory"></param>
        /// <returns>This class.</returns>
        public ChannelClientConfiguration SetSignatureFactory(ISignatureFactory signatureFactory)
        {
            SignatureFactory = signatureFactory;
            return this;
        }

        public ChannelClientConfiguration SetBindingsOutgoing(Bindings bindingsOutgoing)
        {
            BindingsOutgoing = bindingsOutgoing;
            return this;
        }
    }
}
