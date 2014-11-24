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
        public DataBuffer()
        {
            
        }

        public DataBuffer(sbyte[] buffer, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public int AlreadyTransferred()
        {
            throw new NotImplementedException();
        }

        public int TransferFrom(MemoryStream buffer, int remaining)
        {
            throw new NotImplementedException();
        }

        public void TransferTo(MemoryStream buf)
        {
            throw new NotImplementedException();
        }

        internal int Length()
        {
            throw new NotImplementedException();
        }

        // replaces toByteBuf
        public JavaBinaryWriter ToJavaBinaryWriter()
        {
            throw new NotImplementedException();
        }

        public JavaBinaryReader ToJavaBinaryReader()
        {
            throw new NotImplementedException();
        }

        public void ResetAlreadyTransferred()
        {
            throw new NotImplementedException();
        }

        public DataBuffer ShallowCopy()
        {
            throw new NotImplementedException();
        }
    }
}
