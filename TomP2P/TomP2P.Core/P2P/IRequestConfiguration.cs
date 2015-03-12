namespace TomP2P.Core.P2P
{
    public interface IRequestConfiguration
    {
        int Parallel { get; }

        bool IsForceUdp { get; }

        bool IsForceTcp { get; }
    }
}
