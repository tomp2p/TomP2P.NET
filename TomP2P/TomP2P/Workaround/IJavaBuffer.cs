using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Workaround
{
    public interface IJavaBuffer
    {
        bool CanRead { get; }

        int WriterIndex { get; }

        long ReaderIndex { get; }

        sbyte[] Buffer { get; }
    }
}
