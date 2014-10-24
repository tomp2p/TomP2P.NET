using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Peers;
using TomP2P.Workaround;

namespace TomP2P.Storage
{
     /// <summary>
     /// This class holds the data for the transport. The data is already serialized and a hash may be created. 
     /// It is reasonable to create the hash on the remote peer, but not on the local peer. 
     /// The remote peer uses the hash to tell the other peers, which version is stored and its used quite often.
     /// </summary>
     public class Data
    {
         public Data(int i, int i1)
         {
             throw new NotImplementedException();
         }

         public Number160 Hash()
         {
             throw new NotImplementedException();
         }

         public Data Duplicate()
         {
             throw new NotImplementedException();
         }

         public Data DuplicateMeta()
         {
             throw new NotImplementedException();
         }

         public Data SetTtlSeconds(int ttlSeconds)
         {
             throw new NotImplementedException();
         }

         public long ExpirationMillis
         {
             get { throw new InvalidOperationException();}
         }

         public void EncodeHeader(JavaBinaryWriter buffer, ISignatureFactory signatureFactory)
         {
             throw new NotImplementedException();
         }

         public bool EncodeBuffer(JavaBinaryWriter buffer)
         {
             throw new NotImplementedException();
         }

         public void EncodeDone(JavaBinaryWriter buffer, ISignatureFactory signatureFactory, IPrivateKey messagePrivateKey)
         {
             throw new NotImplementedException();
         }

         public bool DecodeBuffer(BinaryReader buffer)
         {
             throw new NotImplementedException();
         }

         public bool DecodeDone(BinaryReader buffer, IPublicKey publicKey, ISignatureFactory signatureFactory)
         {
             throw new NotImplementedException();
         }

         public static Data DecodeHeader(BinaryReader buffer, ISignatureFactory signatureFactory)
         {
             throw new NotImplementedException();
         }

         public IPublicKey PublicKey()
         {
             throw new NotImplementedException();
         }

         public Data PublicKey(IPublicKey publicKey)
         {
             throw new NotImplementedException();
         }

         public bool HasPublicKey()
         {
             throw new NotImplementedException();
         }
    }
}
