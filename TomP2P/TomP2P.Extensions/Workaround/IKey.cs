using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TomP2P.Extensions.Workaround
{
    public interface IKey
    {
        byte[] GetEncoded();
    }
}
