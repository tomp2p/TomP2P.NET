using System.Collections.Generic;
using TomP2P.Core.Peers;
using TomP2P.Extensions.Workaround;

namespace TomP2P.Dht
{
    public class RemoveBuilder : DhtBuilder<RemoveBuilder>, ISearchableBuilder
    {
        private static readonly TcsRemove TcsRemoveShutdown = new TcsRemove(null);

        public ICollection<Number160> ContentKeys { get; private set; }
        public ICollection<Number640> Keys { get; private set; }
        public Number160 ContentKey { get; private set; }

        public bool IsAll { get; private set; }
        public bool IsReturnResults { get; private set; }
        public bool IsFastGet { get; private set; }
        public bool IsFailIfNotFound { get; private set; }

        public Number640 From { get; private set; }
        public Number640 To { get; private set; }

        // static constructor
        static RemoveBuilder()
        {
            TcsRemoveShutdown.SetException(new TaskFailedException("Peer is shutting down."));
        }

        public RemoveBuilder(PeerDht peerDht, Number160 locationKey)
            : base(peerDht, locationKey)
        {
            IsFastGet = true;
            SetSelf(this);
        }

        public TcsRemove Start()
        {
            if (PeerDht.Peer.IsShutdown)
            {
                return TcsRemoveShutdown;
            }
            PreBuild();
            if (IsAll)
            {
                ContentKeys = null;
            }
            else if (ContentKeys == null && !IsAll && !IsRange)
            {
                ContentKeys = new List<Number160>(1);
                if (ContentKey == null)
                {
                    ContentKey = Number160.Zero;
                }
                ContentKeys.Add(ContentKey);
            }
            return PeerDht.Dht.Remove(this);
        }

        public RemoveBuilder SetContentKeys(ICollection<Number160> contentKeys)
        {
            ContentKeys = contentKeys;
            return this;
        }

        public RemoveBuilder SetKeys(ICollection<Number640> keys)
        {
            Keys = keys;
            return this;
        }

        public RemoveBuilder SetContentKey(Number160 contentKey)
        {
            ContentKey = contentKey;
            return this;
        }

        public RemoveBuilder SetIsAll()
        {
            return SetIsAll(true);
        }

        public RemoveBuilder SetIsAll(bool isAll)
        {
            IsAll = isAll;
            return this;
        }

        public RemoveBuilder SetIsReturnResults()
        {
            return SetIsReturnResults(true);
        }

        public RemoveBuilder SetIsReturnResults(bool isReturnResults)
        {
            IsReturnResults = isReturnResults;
            return this;
        }

        public RemoveBuilder SetIsFastGet()
        {
            return SetIsFastGet(true);
        }

        public RemoveBuilder SetIsFastGet(bool isFastGet)
        {
            IsFastGet = isFastGet;
            return this;
        }

        public RemoveBuilder SetIsFailIfNoFound()
        {
            return SetIsFailIfNoFound(true);
        }

        public RemoveBuilder SetIsFailIfNoFound(bool isFailIfNotFound)
        {
            IsFailIfNotFound = isFailIfNotFound;
            return this;
        }

        public RemoveBuilder SetFrom(Number640 from)
        {
            DomainKey = from.DomainKey;
            From = from;
            return this;
        }

        public RemoveBuilder SetTo(Number640 to)
        {
            DomainKey = to.DomainKey;
            To = to;
            return this;
        }

        public bool IsRange
        {
            get { return From != null && To != null; }
        }
    }
}
