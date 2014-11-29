using System;
using System.Data;
using System.Security.Cryptography;
using NLog;
using TomP2P.Extensions.Workaround;
using TomP2P.Message;

namespace TomP2P.Connection
{
    /// <summary>
    /// The default signature is done with SHA1withDSA
    /// </summary>
    public class DsaSignatureFactory : ISignatureFactory
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public void EncodePublicKey(IPublicKey publicKey, JavaBinaryWriter buffer)
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
