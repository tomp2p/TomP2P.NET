using System.IO;

namespace TomP2P.Extensions.Workaround
{
    public interface IJavaBuffer
    {
        Stream BaseStream { get; }

        bool CanRead { get; }

        int WriterIndex { get; }

        long ReaderIndex { get; }

        sbyte[] Buffer { get; }
    }
}
