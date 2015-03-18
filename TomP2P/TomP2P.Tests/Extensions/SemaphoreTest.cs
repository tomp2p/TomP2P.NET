using System;
using System.Threading;
using NUnit.Framework;
using TomP2P.Extensions;

namespace TomP2P.Tests.Extensions
{
    [TestFixture]
    public class SemaphoreTest
    {
        [Test]
        public void ReleaseTest()
        {
            var s = new Semaphore(1, 1);
            s.WaitOne();

            Console.WriteLine("Bla1");
            s.Release2(1);

            s.WaitOne();
            Console.WriteLine("Bla2");
        }
    }
}
