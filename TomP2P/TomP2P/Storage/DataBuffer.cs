using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Storage
{
    public class DataBuffer
    {
        public DataBuffer(sbyte[] buffer, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public int AlreadyTransferred()
        {
            throw new NotImplementedException();
        }

        public int TransferFrom(JavaBinaryReader buffer, int remaining)
        {
            throw new NotImplementedException();
        }

        internal int Length()
        {
            throw new NotImplementedException();
        }
    }
}
