using System;
using System.Security.Cryptography;
using TomP2P.Extensions.Workaround;
using TomP2P.Message;

namespace TomP2P.Connection
{
    /// <summary>
    /// The default signature is done with SHA1withDSA
    /// </summary>
    public class DsaSignatureFactory : ISignatureFactory
    {
        public void EncodePublicKey(IPublicKey publicKey, JavaBinaryWriter buffer)
        {
            throw new NotImplementedException();
        }

        public IPublicKey DecodePublicKey(byte[] me)
        {
            throw new NotImplementedException();
        }

        public IPublicKey DecodePublicKey(JavaBinaryReader buffer)
        {
            throw new NotImplementedException();
        }

        public ISignatureCodec Sign(IPrivateKey privateKey, JavaBinaryWriter buffer)
        {
            throw new NotImplementedException();
        }

        public bool Verify(IPublicKey publicKey, JavaBinaryReader buffer, ISignatureCodec signatureEncoded)
        {
            throw new NotImplementedException();
        }

        public RSACryptoServiceProvider Update(IPublicKey publicKey, System.IO.MemoryStream[] buffers)
        {
            throw new NotImplementedException();
        }

        public ISignatureCodec SignatureCodec
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
