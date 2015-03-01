﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TomP2P.P2P;
using TomP2P.Peers;

namespace TomP2P.Tests.P2P
{
    [TestFixture]
    public class BootstrapTest
    {
        [Test]
        public async void TestBootstrapDiscover()
        {
            var rnd = new Random(42);
            Peer master = null;
            Peer slave = null;
            try
            {
                master = new PeerBuilder(new Number160(rnd)).SetPorts(4001).Start();
                slave = new PeerBuilder(new Number160(rnd)).SetPorts(4002).Start();

                var tcsDiscover = master.Discover().SetPeerAddress(slave.PeerAddress).Start();
                await tcsDiscover.Task;

                Assert.IsTrue(!tcsDiscover.Task.IsFaulted);
            }
            finally
            {
                if (master != null)
                {
                    master.ShutdownAsync().Wait();
                }
                if (slave != null)
                {
                    slave.ShutdownAsync().Wait();
                }
            }
        }
    }
}
