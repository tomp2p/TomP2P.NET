using TomP2P.Peers;

namespace TomP2P.Rpc
{
    public interface IBloomfilterFactory
    {
        SimpleBloomFilter<Number160> CreateContentBloomFilter();

        SimpleBloomFilter<Number160> CreateLocationKeyBloomFilter();

        SimpleBloomFilter<Number160> CreateDomainKeyBloomFilter();

        SimpleBloomFilter<Number160> CreateContentKeyBloomFilter();
    }
}
