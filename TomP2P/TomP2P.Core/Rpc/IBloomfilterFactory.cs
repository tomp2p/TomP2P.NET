using TomP2P.Core.Peers;

namespace TomP2P.Core.Rpc
{
    public interface IBloomfilterFactory
    {
        SimpleBloomFilter<Number160> CreateContentBloomFilter();

        SimpleBloomFilter<Number160> CreateLocationKeyBloomFilter();

        SimpleBloomFilter<Number160> CreateDomainKeyBloomFilter();

        SimpleBloomFilter<Number160> CreateContentKeyBloomFilter();
    }
}
