using TomP2P.Core.Peers;

namespace TomP2P.Core.Rpc
{
    public class DefaultBloomFilterFactory : IBloomfilterFactory
    {
        public SimpleBloomFilter<Number160> CreateContentBloomFilter()
        {
            return new SimpleBloomFilter<Number160>(0.01d, 1000);
        }

        public SimpleBloomFilter<Number160> CreateLocationKeyBloomFilter()
        {
            return new SimpleBloomFilter<Number160>(0.01d, 1000);
        }

        public SimpleBloomFilter<Number160> CreateDomainKeyBloomFilter()
        {
            return new SimpleBloomFilter<Number160>(0.01d, 1000);
        }

        public SimpleBloomFilter<Number160> CreateContentKeyBloomFilter()
        {
            return new SimpleBloomFilter<Number160>(0.01d, 1000);
        }
    }
}
