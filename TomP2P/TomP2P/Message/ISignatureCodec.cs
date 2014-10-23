using System.IO;

namespace TomP2P.Message
{
    public interface ISignatureCodec
    {
        ISignatureCodec Decode(byte[] encodedData); // TODO throws exception?

        byte[] Encode(); // TODO throws exception?

        ISignatureCodec Write(MemoryStream buffer);

        ISignatureCodec Read(BinaryReader buffer);

        int SignatureSize { get; set; }
    }
}