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

    public static class RpcExtensions
    {
        public static sbyte GetNr(this Rpc.Commands command)
        {
            return (sbyte) ((int) command);
        }
    }
}
