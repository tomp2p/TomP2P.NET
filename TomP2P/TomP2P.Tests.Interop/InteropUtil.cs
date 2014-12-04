using TomP2P.Extensions;
using TomP2P.Extensions.Netty;

namespace TomP2P.Tests.Interop
{
    public class InteropUtil
    {
        public static byte[] ExtractBytes(AlternativeCompositeByteBuf buf)
        {
            var buffer = buf.NioBuffer();
            buffer.Position = 0;

            var bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
