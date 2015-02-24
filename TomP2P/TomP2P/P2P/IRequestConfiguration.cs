namespace TomP2P.P2P
{
    public interface IRequestConfiguration
    {
        int Parallel { get; }

        bool IsForceUdp { get; }

        bool IsForceTcp { get; }
    }
}
