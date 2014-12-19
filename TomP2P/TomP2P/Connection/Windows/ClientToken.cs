using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Connection.Windows
{
    public class ClientToken
    {
        public byte[] SendBuffer;
        public byte[] RecvBuffer;

        public ClientToken(int bufferSize)
        {
            SendBuffer = new byte[bufferSize];
            RecvBuffer = new byte[bufferSize];
        }

        public void Reset()
        {
            SendBuffer = new byte[SendBuffer.Length];
            RecvBuffer = new byte[RecvBuffer.Length];
        }
    }
}
