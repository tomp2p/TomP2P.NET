using System;
using System.IO;
using System.Security.Cryptography;
using NLog;
using TomP2P.Core.Message;
using TomP2P.Extensions.Netty.Buffer;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Core.Connection
{
    /// <summary>
    /// The default signature is done with SHA1withDSA
    /// </summary>
    public class DsaSignatureFactory : ISignatureFactory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public void EncodePublicKey(IPublicKey publicKey, ByteBuf buffer)
        {
            var dsa = new DSACryptoServiceProvider();
            var dsap = dsa.ExportParameters(false);


            var rsa = new RSACryptoServiceProvider();
            var rsap = rsa.ExportParameters(false);


        }

        public IPublicKey DecodePublicKey(byte[] me)
        {
            throw new NotImplementedException();
        }

        public IPublicKey DecodePublicKey(ByteBuf buffer)
        {
            throw new NotImplementedException();
        }

        public ISignatureCodec Sign(IPrivateKey privateKey, ByteBuf buffer)
        {
            throw new NotImplementedException();
        }

        public bool Verify(IPublicKey publicKey, ByteBuf buffer, ISignatureCodec signatureEncoded)
        {
            throw new NotImplementedException();
        }

        public RSACryptoServiceProvider Update(IPublicKey publicKey, MemoryStream[] buffers)
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
