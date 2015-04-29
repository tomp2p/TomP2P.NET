using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TomP2P.Core.P2P;
using TomP2P.Core.Rpc;

namespace TomP2P.Dht
{
    public class DistributedHashTable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public const int ReasonCancel = 254;
        public const int ReasonUnknown = 255;

        private readonly DistributedRouting _routing;
        private readonly StorageRpc _storageRpc;
        private readonly DirectDataRpc _directDataRpc;

        public DistributedHashTable(DistributedRouting routing, StorageRpc storageRpc, DirectDataRpc directDataRpc)
        {
            _routing = routing;
            _storageRpc = storageRpc;
            _directDataRpc = directDataRpc;
        }

        public TcsPut Add(AddBuilder builder)
        {
            throw new NotImplementedException();
        }

        public TcsRemove Remove(RemoveBuilder builder)
        {
            throw new NotImplementedException();
        }

        public TcsDigest Digest(DigestBuilder builder)
        {
            throw new NotImplementedException();
        }

        public TcsGet Get(GetBuilder builder)
        {
            throw new NotImplementedException();
        }

        public TcsSend Direct(SendBuilder builder)
        {
            throw new NotImplementedException();
        }
    }
}
