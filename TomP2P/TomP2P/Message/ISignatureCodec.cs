using System.IO;
using TomP2P.Workaround;

namespace TomP2P.Message
{
    public interface ISignatureCodec
    {
        ISignatureCodec Decode(byte[] encodedData); // TODO throws exception?

        byte[] Encode(); // TODO throws exception?

        ISignatureCodec Write(JavaBinaryWriter buffer);

        ISignatureCodec Read(BinaryReader buffer);

        int SignatureSize { get; set; }
    }
}