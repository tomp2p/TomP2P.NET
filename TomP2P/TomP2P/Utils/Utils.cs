using System;
using System.Collections.Generic;
using System.Linq;

namespace TomP2P.Utils
{
    public static class Utils
    {
        public const int IPv4Bytes = 4;         //  32 bits
        public const int IPv6Bytes = 16;        // 128 bits
        public const int ByteBits = 8;
        public const int MaskFf = 0xff;         // 1111 1111
        public const int ByteByteSize = 1;      //   8 bits
        public const int ShortByteSize = 2;     //  16 bits
        public const int IntegerByteSize = 4;   //  32 bits
        public const int LongByteSize = 8;      //  64 bits

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

        #region .NET specific

        public static double GetCurrentMillis()
        {
            var jan1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan ts = DateTime.UtcNow - jan1970;
            return ts.TotalMilliseconds;
        }

        #endregion
    }
}
