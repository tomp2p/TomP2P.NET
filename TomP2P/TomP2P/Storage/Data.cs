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
        public bool IsSigned { private set; get; }
        public bool IsFlag1 { private set; get; }
        public bool IsFlag2 { private set; get; }
        public bool IsProtectedEntry { private set; get; }
        public bool HasPublicKey { private set; get; }
        public bool HasPrepareFlag { private set; get; }
        private bool _ttl;

        // can be added later
        public ISignatureCodec Signature { private set; get; }
        public int TtlSeconds { private set; get; }
        public List<Number160> BasedOnSet { private set; get; }
        public IPublicKey PublicKey { private set; get; }
        public IPrivateKey PrivateKey { private set; get; } // TODO make transient

        // never serialized over the network in this object
        public long ValidFromMillis { private set; get; }
        public bool IsMeta { private set; get; }
        private ISignatureFactory _signatureFactory;
        private Number160 _hash;

        public Data(DataBuffer buffer)
            : this(buffer, buffer.Length())
        { }

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
            ValidFromMillis = Convenient.CurrentTimeMillis();
            TtlSeconds = -1;
            BasedOnSet = new List<Number160>(0);
        }

        /// <summary>
        /// Creates an empty Data object. The data can be filled at a later stage.
        /// </summary>
        /// <param name="header">The 8 bit header.</param>
        /// <param name="length">The length, depending on the header values.</param>
        public Data(int header, int length)
        {
            HasPublicKey = CheckHasPublicKey(header);
            IsFlag1 = CheckIsFlag1(header);
            IsFlag2 = CheckIsFlag2(header);
            _basedOnFlag = CheckHasBasedOn(header);
            IsSigned = CheckIsSigned(header);
            _ttl = CheckHasTtl(header);
            IsProtectedEntry = CheckIsProtectedEntry(header);
            _type = Type(header);
            HasPrepareFlag = CheckHasPrepareFlag(header);

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
            ValidFromMillis = Convenient.CurrentTimeMillis();
            TtlSeconds = -1;
            BasedOnSet = new List<Number160>(0);
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
            ValidFromMillis = Convenient.CurrentTimeMillis();
            TtlSeconds = -1;
            BasedOnSet = new List<Number160>(0);
        }

        public bool IsEmpty
        {
            get { return Length == 0; }
        }

        public void EncodeHeader(JavaBinaryWriter buffer, ISignatureFactory signatureFactory)
        {
            var header = (int)_type; // check if works
            if (HasPrepareFlag)
            {
                header |= 0x02;
            }
            if (IsFlag1)
            {
                header |= 0x04;
            }
            if (IsFlag2)
            {
                header |= 0x08;
            }
            if (_ttl)
            {
                header |= 0x10;
            }
            if (IsSigned && HasPublicKey && IsProtectedEntry)
            {
                header |= (0x20 | 0x40);
            }
            else if (IsSigned && HasPublicKey)
            {
                header |= 0x40;
            }
            else if (HasPublicKey)
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
                    buffer.WriteByte((sbyte)header); // TODO check if works
                    buffer.WriteByte((sbyte)Length);
                    break;
                case DataType.Large:
                    buffer.WriteByte((sbyte)header); // TODO check if works
                    buffer.WriteInt(Length);
                    break;
                default:
                    throw new ArgumentException("Unknown DataType.");
            }
            if (_ttl)
            {
                buffer.WriteInt(TtlSeconds);
            }
            if (_basedOnFlag)
            {
                buffer.WriteByte((sbyte)(BasedOnSet.Count() - 1)); // TODO check if works
                foreach (var basedOn in BasedOnSet)
                {
                    buffer.WriteBytes(basedOn.ToByteArray());
                }
            }
            if (HasPublicKey)
            {
                if (PublicKey == null)
                {
                    buffer.WriteShort(0);
                }
                else
                {
                    signatureFactory.EncodePublicKey(PublicKey, buffer);
                }
            }
        }

        // TODO use correct buffer
        public bool EncodeBuffer(MemoryStream buf)
        {
            int already = _buffer.AlreadyTransferred();
            int remaining = Length - already;

            if (remaining == 0)
            {
                // already finished
                return true;
            }

            _buffer.TransferTo(buf);
            return _buffer.AlreadyTransferred() == Length;
        }

        public void EncodeDone(JavaBinaryWriter buf, ISignatureFactory signatureFactory)
        {
            EncodeDone(buf, signatureFactory, null);
        }

        public void EncodeDone(JavaBinaryWriter buf, ISignatureFactory signatureFactory, IPrivateKey messagePrivateKey)
        {
            if (IsSigned)
            {
                if (Signature == null && PrivateKey != null)
                {
                    Signature = signatureFactory.Sign(PrivateKey, _buffer.ToJavaBinaryWriter());
                }
                else if (Signature == null && messagePrivateKey != null)
                {
                    Signature = signatureFactory.Sign(messagePrivateKey, _buffer.ToJavaBinaryWriter());
                }
                else if (Signature == null)
                {
                    throw new ArgumentException("A private key is required to sign.");
                }
                Signature.Write(buf);
            }
        }

        // TODO getter for buffer (maybe both)

        public Object Object
        {
            get
            {
                // TODO implement
                throw new NotImplementedException();
            }
        }

        public Data SetValidFromMillis(long validFromMillis)
        {
            ValidFromMillis = validFromMillis;
            return this;
        }

        public Data SignNow(KeyPair keyPair, ISignatureFactory signatureFactory)
        {
            return SignNow(keyPair, signatureFactory, false);
        }

        public Data ProtectEntryNow(KeyPair keyPair, ISignatureFactory signatureFactory)
        {
            return SignNow(keyPair, signatureFactory, true);
        }

        private Data SignNow(KeyPair keyPair, ISignatureFactory signatureFactory, bool isProtectedEntry)
        {
            if (Signature == null)
            {
                Signature = signatureFactory.Sign(keyPair.PrivateKey, _buffer.ToJavaBinaryWriter());
                IsSigned = true;
                PublicKey = keyPair.PublicKey;
                HasPublicKey = true;
                IsProtectedEntry = isProtectedEntry;
            }
            return this;
        }

        public Data SignNow(IPrivateKey privateKey, ISignatureFactory signatureFactory)
        {
            return SignNow(privateKey, signatureFactory, false);
        }

        public Data ProtectEntryNow(IPrivateKey privateKey, ISignatureFactory signatureFactory)
        {
            return SignNow(privateKey, signatureFactory, true);
        }

        private Data SignNow(IPrivateKey privateKey, ISignatureFactory signatureFactory, bool isProtectedEntry)
        {
            if (Signature == null)
            {
                Signature = signatureFactory.Sign(privateKey, _buffer.ToJavaBinaryWriter());
                IsSigned = true;
                HasPublicKey = true;
                IsProtectedEntry = isProtectedEntry;
            }
            return this;
        }

        public Data Sign(KeyPair keyPair)
        {
            return Sign(keyPair, false);
        }

        public Data ProtectEntry(KeyPair keyPair, bool isProtectedEntry)
        {
            return Sign(keyPair, true);
        }

        private Data Sign(KeyPair keyPair, bool isProtectedEntry)
        {
            IsSigned = true;
            PrivateKey = keyPair.PrivateKey;
            PublicKey = keyPair.PublicKey;
            HasPublicKey = true;
            IsProtectedEntry = isProtectedEntry;
            return this;
        }

        public Data Sign()
        {
            return Sign((IPrivateKey)null, false);
        }

        public Data Sign(IPrivateKey privateKey)
        {
            return Sign(privateKey, false);
        }

        public Data ProtectEntry()
        {
            return Sign((IPrivateKey)null, true);
        }

        public Data ProtectEntry(IPrivateKey privateKey)
        {
            return Sign(privateKey, true);
        }

        private Data Sign(IPrivateKey privateKey, bool isProtectedEntry)
        {
            IsSigned = true;
            PrivateKey = privateKey;
            HasPublicKey = true;
            IsProtectedEntry = isProtectedEntry;
            return this;
        }

        public long ExpirationMillis
        {
            get
            {
                return TtlSeconds <= 0 ? Convenient.JavaLongMaxValue : ValidFromMillis + (TtlSeconds * 1000L);
            }
        }

        public Data SetTtlSeconds(int ttlSeconds)
        {
            TtlSeconds = ttlSeconds;
            _ttl = true;
            return this;
        }

        public Data AddBasedOn(Number160 basedOn)
        {
            BasedOnSet.Add(basedOn);
            _basedOnFlag = true;
            return this;
        }

        public ISignatureFactory SignatureFactory
        {
            get
            {
                if (_signatureFactory == null)
                {
                    return new DsaSignatureFactory();
                }
                return _signatureFactory;
            }
        }

        public Data SetSignatureFactory(ISignatureFactory signatureFactory)
        {
            _signatureFactory = signatureFactory;
            return this;
        }

        public Data SetIsSigned()
        {
            return SetIsSigned(true);
        }

        public Data SetIsSigned(bool isSigned)
        {
            IsSigned = isSigned;
            HasPublicKey = true;
            return this;
        }

        public Data SetIsFlag1()
        {
            return SetIsFlag1(true);
        }

        public Data SetIsFlag1(bool isFlag1)
        {
            if (isFlag1 && IsFlag2)
            {
                throw new ArgumentException("Cannot set both flags. This means that data is deleted.");
            }
            IsFlag1 = isFlag1;
            return this;
        }

        public Data SetIsFlag2()
        {
            return SetIsFlag2(true);
        }

        public Data SetIsFlag2(bool isFlag2)
        {
            if (isFlag2 && IsFlag1)
            {
                throw new ArgumentException("Cannot set both flags. This means that data is deleted.");
            }
            IsFlag2 = isFlag2;
            return this;
        }

        public Data SetHasPreparaFlag()
        {
            return SetHasPreparaFlag(true);
        }

        public Data SetHasPreparaFlag(bool hasPrepareFlag)
        {
            HasPrepareFlag = hasPrepareFlag;
            return this;
        }

        public Data SetIsDeleted(bool isDeleted)
        {
            if (IsFlag1 || IsFlag2)
            {
                throw new ArgumentException("Cannot set deleted, because one flag is already set.");
            }
            IsFlag1 = isDeleted;
            IsFlag2 = isDeleted;
            return this;
        }

        public bool IsDeleted
        {
            get { return IsFlag1 && IsFlag2; }
        }

        public Data SetHasPublicKey()
        {
            return SetHasPublicKey(true);
        }

        public Data SetHasPublicKey(bool hasPublicKey)
        {
            HasPublicKey = hasPublicKey;
            return this;
        }

        public Data SetIsMeta()
        {
            return SetIsMeta(true);
        }

        public Data SetIsMeta(bool isMeta)
        {
            IsMeta = isMeta;
            return this;
        }

        public Data SetPublicKey(IPublicKey publicKey)
        {
            PublicKey = publicKey;
            HasPublicKey = true;
            return this;
        }

        public Data SetSignature(ISignatureCodec signature)
        {
            Signature = signature;
            return this;
        }

        public void ResetAlreadyTransferred()
        {
            _buffer.ResetAlreadyTransferred();
        }

        public Data Duplicate()
        {
            var data = new Data(_buffer.ShallowCopy(), Length)
                .SetPublicKey(PublicKey)
                .SetSignature(Signature)
                .SetTtlSeconds(TtlSeconds);

            // duplicate based on keys
            data.BasedOnSet.AddRange(BasedOnSet);

            // duplicate all the flags. 
            // although signature, basedOn, and ttlSeconds set a flag, they will be overwritten with the data from this class
            data.HasPublicKey = HasPublicKey;
            data.IsFlag1 = IsFlag1;
            data.IsFlag2 = IsFlag2;
            data._basedOnFlag = _basedOnFlag;
            data.IsSigned = IsSigned;
            data._ttl = _ttl;
            data.IsProtectedEntry = IsProtectedEntry;
            data.PrivateKey = PrivateKey;
            data.ValidFromMillis = ValidFromMillis;
            data.HasPrepareFlag = HasPrepareFlag;
            return data;
        }

        public Data DuplicateMeta()
        {
            var data = new Data()
                .SetPublicKey(PublicKey)
                .SetSignature(Signature)
                .SetTtlSeconds(TtlSeconds);

            // duplicate based on keys
            data.BasedOnSet.AddRange(BasedOnSet);

            // duplicate all the flags. 
            // although signature, basedOn, and ttlSeconds set a flag, they will be overwritten with the data from this class
            data.HasPublicKey = HasPublicKey;
            data.IsFlag1 = IsFlag1;
            data.IsFlag2 = IsFlag2;
            data._basedOnFlag = _basedOnFlag;
            data.IsSigned = IsSigned;
            data._ttl = _ttl;
            data.IsProtectedEntry = IsProtectedEntry;
            data.PrivateKey = PrivateKey;
            data.ValidFromMillis = ValidFromMillis;
            data.HasPrepareFlag = HasPrepareFlag;
            return data;
        }



        public Number160 Hash()
        {
            throw new NotImplementedException();
        }

        public bool EncodeBuffer(JavaBinaryWriter buffer)
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

        public override string ToString()
        {
            var sb = new StringBuilder("Data[l:");
            sb.Append(Length)
                .Append(",t:").Append(TtlSeconds)
                .Append(",hasPK:").Append(PublicKey != null)
                .Append(",h:").Append(Signature).Append("]");
            return sb.ToString();
        }

        public static Data DecodeHeader(JavaBinaryReader buffer, ISignatureFactory signatureFactory)
        {
            throw new NotImplementedException();
        }

        public static DataType Type(int header)
        {
            return (DataType)(header & 0x1); // TODO check if works
        }

        private static bool CheckHasPrepareFlag(int header)
        {
            return (header & 0x02) > 0;
        }

        private static bool CheckIsFlag1(int header)
        {
            return (header & 0x04) > 0;
        }

        private static bool CheckIsFlag2(int header)
        {
            return (header & 0x08) > 0;
        }

        private static bool CheckHasTtl(int header)
        {
            return (header & 0x10) > 0;
        }

        private static bool CheckHasPublicKey(int header)
        {
            return ((header >> 5) & (0x01 | 0x02)) > 0;
        }

        private static bool CheckIsProtectedEntry(int header)
        {
            return ((header >> 5) & (0x01 | 0x02)) > 2;
        }

        private static bool CheckIsSigned(int header)
        {
            return ((header >> 5) & (0x01 | 0x02)) > 1;
        }

        private static bool CheckHasBasedOn(int header)
        {
            return (header & 0x80) > 0;
        }
    }
}
