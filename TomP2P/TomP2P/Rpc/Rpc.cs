namespace TomP2P.Rpc
{
    public class Rpc
    {
        // TODO convert to Java-like enum
        // max. 255 commands
        public enum Commands
        {
            Ping,
            Put,
            Get,
            Add,
            Remove,
            Neighbor,
            Quit,
            DirectData,
            TrackerAdd,
            TrackerGet,
            Pex,
            Digest,
            Broadcast,
            PutMeta,
            DigestBloomfilter,
            Relay,
            DigestMetaValues,
            Sync,
            SyncInfo,
            PutConfirm,
            GetLatest,
            Rcon,
            GetLatestWithDigest
        }
    }
}
