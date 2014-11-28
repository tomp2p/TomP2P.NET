using System;
using NUnit.Framework;
using TomP2P.Extensions;

namespace TomP2P.Tests.Extensions
{
    [TestFixture]
    public class InteropRandomTest
    {
        [Test]
        public void TestSeed()
        {
            // create a random seed
            var r = new Random();
            var seed = (ulong)r.Next();

            int tests = 20;
            var randoms = new InteropRandom[tests];
            var results = new int[tests];

            for (int i = 0; i < tests; i++)
            {
                randoms[i] = new InteropRandom(seed);
                results[i] = randoms[i].NextInt(1000);

                if (i > 0)
                {
                    Assert.AreEqual(results[i], results[i - 1]);
                }
            }
        }

        [Test]
        public void TestInteropSeed()
        {
            // use the same seed as in Java
            const int seed = 1234567890;
            var random = new InteropRandom(seed);
            var result1 = random.NextInt(1000);
            var result2 = random.NextInt(500);
            var result3 = random.NextInt(10);
          
            // requires same results as in Java
            // result1 is 677
            // result2 is 242
            // result3 is 1
            Assert.AreEqual(result1, 677);
            Assert.AreEqual(result2, 242);
            Assert.AreEqual(result3, 1);
        }
    }
}
