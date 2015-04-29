namespace TomP2P.Core.Rpc
{
    public class Rpc
    {
        // TODO convert to Java-like enum
        // max. 255 commands
        // Don't change the order! Keep Java interoperability in mind.
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
            HoleP,
            GetLatestWithDigest,
            Gcm,
            LocalAnnounce,
            ReplicaPut,
            DigestAllBloomfilter
        }
    }

    public static class RpcExtensions
    {
        public static sbyte GetNr(this Rpc.Commands command)
        {
            return (sbyte) ((int) command);
        }
    }
}
