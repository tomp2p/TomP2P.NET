using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomP2P.Rpc;

namespace TomP2P.Peers
{
    public interface IBloomfilterFactory
    {
        SimpleBloomFilter<Number160> CreateContentBloomFilter();

        SimpleBloomFilter<Number160> CreateLocationKeyBloomFilter();

        SimpleBloomFilter<Number160> CreateDomainKeyBloomFilter();

        SimpleBloomFilter<Number160> CreateContentKeyBloomFilter();
    }
}
