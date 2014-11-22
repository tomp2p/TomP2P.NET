using System.IO;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Message
{
    public interface ISignatureCodec
    {
        ISignatureCodec Decode(byte[] encodedData); // TODO throws exception?

        byte[] Encode(); // TODO throws exception?

        ISignatureCodec Write(JavaBinaryWriter buffer);

        ISignatureCodec Read(JavaBinaryReader buffer);

        int SignatureSize { get; set; }
    }
}