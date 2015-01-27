﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TomP2P.Connection;
using TomP2P.Extensions;
using TomP2P.Extensions.Netty;
using TomP2P.Extensions.Workaround;
using TomP2P.Peers;
using TomP2P.Storage;

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

        public static readonly sbyte[] EmptyByteArray = new sbyte[0];

        public new static bool Equals(Object a, Object b)
        {
            // TODO check if works
            return (a == b) || (a != null && a.Equals(b));
        }

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

        public static IPAddress Inet4AddressFromBytes(sbyte[] src, long offset)
        {
            byte[] tmp = new byte[IPv4Bytes];
            byte[] src2 = src.ToByteArray();

            Array.Copy(src2, offset, tmp, 0, IPv4Bytes);

            return new IPAddress(tmp);
        }

        public static IPAddress Inet6AddressFromBytes(sbyte[] src, long offset)
        {
            var tmp = new byte[IPv6Bytes];
            byte[] src2 = src.ToByteArray();

            Array.Copy(src2, offset, tmp, 0, IPv6Bytes);

            return new IPAddress(tmp);
        }

        public static Number160 MakeShaHash(ByteBuf buf)
        {
            throw new NotImplementedException();

            // see http://stackoverflow.com/questions/6843698/calculating-sha-1-hashes-in-java-and-c-sharp
            // TODO implement
            // TODO check if works
            /*byte[] buffer = Convenient.ReadFully(br.BaseStream, 0);

            var md = SHA1.Create();
            byte[] digest = md.ComputeHash(buffer); // stream could be passed

            return new Number160(digest.ToSByteArray()); // TODO make unit test
            */
        }

        public static Number160 MakeShaHash(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a listener to the response tasks and releases all acquired channels in the channel creator.
        /// </summary>
        /// <param name="channelCreator">The channel creator that will be shutdown and all connections will be closed.</param>
        /// <param name="tasks">The tasks to listen to. If all the tasks finished, then the channel creator is shut down.
        /// If null is provided, then the channel crator is shut down immediately.</param>
        public static void AddReleaseListener(ChannelCreator channelCreator, params Task[] tasks)
        {
            if (tasks == null)
            {
                channelCreator.ShutdownAsync();
                return;
            }
            int count = tasks.Count();
            var finished = new AtomicInteger(0);
            foreach (var task in tasks)
            {
                task.ContinueWith(delegate
                {
                    if (finished.IncrementAndGet() == count)
                    {
                        channelCreator.ShutdownAsync();
                    }
                });
            }
        }

        #region .NET only

        /// <summary>
        /// Evaluates a reasonable number of clients that can be served on a server on this machine.
        /// NrOfClients = #cores + 1
        /// </summary>
        public static int GetMaxNrOfClients()
        {
            int coreCount = 0;
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                coreCount += int.Parse(item["NumberOfCores"].ToString());
            }
            return coreCount + 1;
        }

        #endregion
    }
}
