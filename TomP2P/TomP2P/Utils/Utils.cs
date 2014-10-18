using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomP2P.Utils
{
    public class Utils
    {
        public const int IPv4Bytes = 4;
        public const int IPv6Bytes = 16;
        public const int MaskFf = 0xff;


        // TODO correct use of generics?
        public static bool IsSameSets<T>(ICollection<T> set1, ICollection<T> set2)
        {
            if (set1 == null ^ set2 == null) // XOR
            {
                return false;
            }
            if (set1 != null && (set1.Count != set2.Count))
            {
                return false;
            }
            if (set1 != null && (set1.Any(obj => !set2.Contains(obj))))
            {
                return false;
            }
            return true;
        }
    }
}
