using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;

namespace TomP2P.Utils
{
    public static class Utils
    {
        public const int IPv4Bytes = 4;         //  32 bits
        public const int IPv6Bytes = 16;        // 128 bits
        public const int ByteBits = 8;
        public const int Mask0F = 0xf;          // 00000000 00000000 00000000 00001111
        public const int Mask80 = 0x80;         // 00000000 00000000 00000000 10000000
        public const int MaskFf = 0xff;         // 00000000 00000000 00000000 11111111
        public const int ByteByteSize = 1;      //   8 bits
        public const int ShortByteSize = 2;     //  16 bits
        public const int IntegerByteSize = 4;   //  32 bits
        public const int LongByteSize = 8;      //  64 bits

        public static bool IsSameSets<T>(IEnumerable<T> set1, IEnumerable<T> set2)
        {
            if (set1 == null ^ set2 == null) // XOR
            {
                return false;
            }
            if (set1 != null && (set1.Count() != set2.Count()))
            {
                return false;
            }
            if (set1 != null && (set1.Any(obj => !set2.Contains(obj))))
            {
                return false;
            }
            return true;
        }

        public static bool IsSameCollectionSets<T>(IEnumerable<IEnumerable<T>> set1, IEnumerable<IEnumerable<T>> set2)
        {
            if (set1 == null ^ set2 == null)
            {
                return false;
            }
            if (set1 != null && (set1.Count() != set2.Count()))
            {
                return false;
            }
            if (set1 != null)
            {
                foreach (var collection1 in set1)
                {
                    foreach (var collection2 in set2)
                    {
                        if (!CollectionsContainCollection(set1, collection2))
                        {
                            return false;
                        }
                        if (!CollectionsContainCollection(set2, collection1))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private static bool CollectionsContainCollection<T>(IEnumerable<IEnumerable<T>> collections, IEnumerable<T> collection)
        {
            foreach (var col in collections)
            {
                if (IsSameSets(collection, col))
                {
                    return true;
                }
            }
            return false;
        }

        #region .NET specific

        public static double GetCurrentMillis()
        {
            var jan1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan ts = DateTime.UtcNow - jan1970;
            return ts.TotalMilliseconds;
        }

        #endregion

        public static IPAddress Inet4AddressFromBytes(sbyte[] src, long offset)
        {
            var tmp = new byte[IPv4Bytes];
            Array.Copy(src, offset, tmp, 0, IPv4Bytes);

            return new IPAddress(tmp); // TODO test
        }

        public static IPAddress Inet6AddressFromBytes(sbyte[] src, long offset)
        {
            var tmp = new byte[IPv6Bytes];
            Array.Copy(src, offset, tmp, 0, IPv6Bytes);

            return new IPAddress(tmp); // TODO test
        }

    }
}
