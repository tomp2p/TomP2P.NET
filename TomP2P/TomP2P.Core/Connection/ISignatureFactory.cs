using System.IO;
using System.Security.Cryptography;
using TomP2P.Core.Message;
using TomP2P.Extensions.Netty.Buffer;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Core.Connection
{
    /// <summary>
    /// This interface is used in the encoders and decoders.
    /// A user may set its own signature algorithm.
    /// </summary>
    public interface ISignatureFactory
    {
        void EncodePublicKey(IPublicKey publicKey, ByteBuf buffer);

        /// <summary>
        /// The public key is sent over the wire, thus the decoding of it needs special handling.
        /// </summary>
        /// <param name="me">The byte array that contains the public key.</param>
        /// <returns>The decoded public key.</returns>
        IPublicKey DecodePublicKey(byte[] me);

        IPublicKey DecodePublicKey(ByteBuf buffer);

        ISignatureCodec Sign(IPrivateKey privateKey, ByteBuf buffer);

        bool Verify(IPublicKey publicKey, ByteBuf buffer, ISignatureCodec signatureEncoded);

        RSACryptoServiceProvider Update(IPublicKey publicKey, MemoryStream[] buffers);

        ISignatureCodec SignatureCodec { get; set; }
    }
}
