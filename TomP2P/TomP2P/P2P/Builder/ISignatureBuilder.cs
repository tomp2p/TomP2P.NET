using TomP2P.Extensions.Workaround;

namespace TomP2P.P2P.Builder
{
    public interface ISignatureBuilder<T> where T : ISignatureBuilder<T>
    {
        /// <summary>
        /// Indicates whether a message should be signed or not.
        /// </summary>
        bool IsSign { get; }

        /// <summary>
        /// Gets the current key pair used to sign the message. If null, no signature is applied.
        /// </summary>
        KeyPair KeyPair { get; }

        /// <summary>
        /// Sets a message to be signed.
        /// </summary>
        /// <returns>This instance.</returns>
        T SetSign();

        /// <summary>
        /// Sets whether a message should be signed or not.
        /// For protecting an entry, this needs to be set to true.
        /// </summary>
        /// <param name="signMessage">True, if the message should be signed.</param>
        /// <returns>This instance.</returns>
        T SetSign(bool signMessage);

        /// <summary>
        /// Sets the key pair to sing the message. The key will be attached to the message and stored
        /// potentially with a data object (if there is such an object in the message).
        /// </summary>
        /// <param name="keyPair">The key pair to be used for signing.</param>
        /// <returns></returns>
        T SetKeyPair(KeyPair keyPair);
    }
}
