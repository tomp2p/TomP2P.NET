using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Extensions;
using TomP2P.Extensions.Workaround;
using TomP2P.Message;
using TomP2P.Peers;

namespace TomP2P.Storage
{
     /// <summary>
     /// This class holds the data for the transport. The data is already serialized and a hash may be created. 
     /// It is reasonable to create the hash on the remote peer, but not on the local peer. 
     /// The remote peer uses the hash to tell the other peers, which version is stored and it's used quite often.
     /// </summary>
     public class Data
     {
         private const int MaxByteSize = 256;

         /// <summary>
         /// Small: 8 bit, Large: 32 bit.
         /// </summary>
         public enum DataType { Small, Large }

         private readonly DataType _type;
         public int Length { private set; get; }
         private readonly DataBuffer _buffer; // contains data without the header

         // these flags can be modified
         private bool _basedOnFlag;
         private bool _signed;
         private bool _flag1;
         private bool _flag2;
         private bool _ttl;
         private bool _protectedEntry;
         private bool _publicKeyFlag;
         private bool _prepareFlag;

         // can be added later
         private ISignatureCodec _signature;
         private int _ttlSeconds = -1;
         private IEnumerable<Number160> _basedOnSet = new List<Number160>(0);
         private IPublicKey _publicKey;
         private IPrivateKey _privateKey; // TODO make transient

         // never serialized over the network in this object
         private long _validFromMillis;
         private ISignatureFactory _signatureFactory;
         private Number160 _hash;
         private bool _meta;

         public Data(DataBuffer buffer)
             : this(buffer, buffer.Length())
         {}

         /// <summary>
         /// Creates a Data object that does have the complete data, but not the complete header.
         /// </summary>
         /// <param name="buffer">The buffer containing the data.</param>
         /// <param name="length">The expected length of the buffer. This does not include the header + size (2, 5 or 9).</param>
         public Data(DataBuffer buffer, int length)
         {
             Length = length;
             _type = Length < MaxByteSize ? DataType.Small : DataType.Large;
             _buffer = buffer;
             _validFromMillis = Convenient.CurrentTimeMillis();
         }

         /// <summary>
         /// Creates an empty Data object. The data can be filled at a later stage.
         /// </summary>
         /// <param name="header">The 8 bit header.</param>
         /// <param name="length">The length, depending on the header values.</param>
         public Data(int header, int length)
         {
             _publicKeyFlag = HasPublicKey(header);
             _flag1 = IsFlag1(header);
             _flag2 = IsFlag2(header);
             _basedOnFlag = HasBasedOn(header);
             _signed = IsSigned(header);
             _ttl = HasTtl(header);
             _protectedEntry = IsProtectedEntry(header);
             _type = Type(header);
             _prepareFlag = HasPrepareFlag(header);

             if (_type == DataType.Small && Length > 255)
             {
                 throw new ArgumentException("DataType is small, but should be large.");
             }
             else if (_type == DataType.Large && (Length <= 255))
             {
                 throw new ArgumentException("DataType is large, but should be small.");
             }

             Length = length;
             _buffer = new DataBuffer();
             _validFromMillis = Convenient.CurrentTimeMillis();
         }

         public Data(Object obj)
         {
             // TODO implement
             throw new NotImplementedException();
         }

         public Data(sbyte[] buffer)
             : this(buffer, 0, buffer.Length)
         { }

         public Data()
             : this(Utils.Utils.EmptyByteArray)
         { }

         /// <summary>
         /// Creates a Data object from an already existing buffer.
         /// </summary>
         /// <param name="buffer"></param>
         /// <param name="offset"></param>
         /// <param name="length"></param>
         public Data(sbyte[] buffer, int offset, int length)
         {
             if (buffer.Length == 0)
             {
                 _buffer = new DataBuffer();
             }
             else
             {
                 _buffer = new DataBuffer(buffer, offset, length);
             }
             Length = length;
             _type = Length < MaxByteSize ? DataType.Small : DataType.Large;
             _validFromMillis = Convenient.CurrentTimeMillis();
         }

         public bool IsEmpty
         {
             get { return Length == 0; }
         }

         public void EncodeHeader(JavaBinaryWriter buffer, ISignatureFactory signatureFactory)
         {
             var header = (int) _type; // check if works
             if (_prepareFlag)
             {
                 header |= 0x02;
             }
             if (_flag1)
             {
                 header |= 0x04;
             }
             if (_flag2)
             {
                 header |= 0x08;
             }
             if (_ttl)
             {
                 header |= 0x10;
             }
             if (_signed && _publicKeyFlag && _protectedEntry)
             {
                 header |= (0x20 | 0x40);
             }
             else if (_signed && _publicKeyFlag)
             {
                 header |= 0x40;
             }
             else if (_publicKeyFlag)
             {
                 header |= 0x20;
             }
             if (_basedOnFlag)
             {
                 header |= 0x80;
             }
             switch (_type)
             {
                 case DataType.Small:
                     buffer.WriteByte((sbyte) header); // TODO check if works
                     buffer.WriteByte((sbyte) Length);
                     break;
                 case DataType.Large:
                     buffer.WriteByte((sbyte) header); // TODO check if works
                     buffer.WriteInt(Length);
                     break;
                 default:
                     throw new ArgumentException("Unknown DataType.");
             }
             if (_ttl)
             {
                 buffer.WriteInt(_ttlSeconds);
             }
             if (_basedOnFlag)
             {
                 buffer.WriteByte((sbyte) (_basedOnSet.Count() -1)); // TODO check if works
                 foreach (var basedOn in _basedOnSet)
                 {
                     buffer.WriteBytes(basedOn.ToByteArray());
                 }
             }
             if (_publicKeyFlag)
             {
                 if (_publicKey == null)
                 {
                     buffer.WriteShort(0);
                 }
                 else
                 {
                     signatureFactory.EncodePublicKey(_publicKey, buffer);
                 }
             }
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

         public bool EncodeBuffer(JavaBinaryWriter buffer)
         {
             throw new NotImplementedException();
         }

         public void EncodeDone(JavaBinaryWriter buffer, ISignatureFactory signatureFactory, IPrivateKey messagePrivateKey)
         {
             throw new NotImplementedException();
         }

         public bool DecodeBuffer(JavaBinaryReader buffer)
         {
             throw new NotImplementedException();
         }

         public bool DecodeDone(JavaBinaryReader buffer, IPublicKey publicKey, ISignatureFactory signatureFactory)
         {
             throw new NotImplementedException();
         }

         public static Data DecodeHeader(JavaBinaryReader buffer, ISignatureFactory signatureFactory)
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

         public static DataType Type(int header)
         {
             return (DataType)(header & 0x1); // TODO check if works
         }

         private static bool HasPrepareFlag(int header)
         {
             return (header & 0x02) > 0;
         }

         private static bool IsFlag1(int header)
         {
             return (header & 0x04) > 0;
         }

         private static bool IsFlag2(int header)
         {
             return (header & 0x08) > 0;
         }

         private static bool HasTtl(int header)
         {
             return (header & 0x10) > 0;
         }

         private static bool HasPublicKey(int header)
         {
             return ((header >> 5) & (0x01 | 0x02)) > 0;
         }

         private static bool IsProtectedEntry(int header)
         {
             return ((header >> 5) & (0x01 | 0x02)) > 2;
         }
         
         private static bool IsSigned(int header)
         {
             return ((header >> 5) & (0x01 | 0x02)) > 1;
         }
         
         private static bool HasBasedOn(int header) {
             return (header & 0x80) > 0;
         }
    }
}
