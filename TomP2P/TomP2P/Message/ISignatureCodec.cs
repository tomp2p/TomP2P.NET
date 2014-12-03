using TomP2P.Extensions.Netty;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Message
{
    public interface ISignatureCodec
    {
        byte[] Encode(); // TODO throws exception?

        ISignatureCodec Decode(byte[] encodedData); // TODO throws exception?

        ISignatureCodec Write(ByteBuf buffer);

        ISignatureCodec Read(ByteBuf buffer);

        int SignatureSize { get; set; }
    }
}