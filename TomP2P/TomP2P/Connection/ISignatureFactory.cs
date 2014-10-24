using System.IO;
using System.Security.Cryptography;
using TomP2P.Message;
using TomP2P.Workaround;

namespace TomP2P.Connection
{
    /// <summary>
    /// This interface is used in the encoders and decoders.
    /// A user may set its own signature algorithm.
    /// </summary>
    public interface ISignatureFactory
    {
        /// <summary>
        /// The public key is sent over the wire, thus the decoding of it needs special handling.
        /// </summary>
        /// <param name="me">The byte array that contains the public key.</param>
        /// <returns>The decoded public key.</returns>
        IPublicKey DecodePublicKey(byte[] me);

        IPublicKey DecodePublicKey(BinaryReader buffer);

        void EncodePublicKey(IPublicKey publicKey, JavaBinaryWriter buffer);

        ISignatureCodec Sign(IPrivateKey privateKey, JavaBinaryWriter buffer); // TODO throw exception?

        bool Verify(IPublicKey publicKey, MemoryStream buffer, ISignatureCodec signatureEncoded); // TODO throw exception?

        RSACryptoServiceProvider Update(IPublicKey publicKey, MemoryStream[] buffers); // TODO throw exception?

        ISignatureCodec SignatureCodec { get; set; }
    }
}
