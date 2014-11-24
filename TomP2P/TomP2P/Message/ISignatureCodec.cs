using TomP2P.Extensions.Workaround;

namespace TomP2P.Message
{
    public interface ISignatureCodec
    {
        byte[] Encode(); // TODO throws exception?

        ISignatureCodec Decode(byte[] encodedData); // TODO throws exception?

        ISignatureCodec Write(JavaBinaryWriter buffer);

        ISignatureCodec Read(JavaBinaryReader buffer);

        int SignatureSize { get; set; }
    }
}