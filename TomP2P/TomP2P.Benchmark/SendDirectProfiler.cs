using System.Threading.Tasks;
using TomP2P.Core.Connection;
using TomP2P.Core.Message;
using TomP2P.Core.P2P;
using TomP2P.Core.P2P.Builder;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Core.Storage;

namespace TomP2P.Benchmark
{
    public abstract class SendDirectProfiler : Profiler
    {
        private const int BufferSizeBytes = 1000;
        protected readonly bool IsForceUdp;
        protected Peer Sender;
        protected Peer Receiver;
        protected ChannelCreator Cc;
        protected SendDirectBuilder SendDirectBuilder;

        protected SendDirectProfiler(bool isForceUdp)
        {
            IsForceUdp = isForceUdp;
        }

        protected override async Task ShutdownAsync()
        {
            if (Sender != null)
            {
                await Sender.ShutdownAsync();
            }
            if (Receiver != null)
            {
                await Receiver.ShutdownAsync();
            }
            if (Cc != null)
            {
                await Cc.ShutdownAsync();
            }
        }

        protected static Buffer CreateSampleBuffer()
        {
            var acbb = AlternativeCompositeByteBuf.CompBuffer();
            for (int i = 0; i < BufferSizeBytes; i++)
            {
                acbb.WriteByte(i%256);
            }
            return new Buffer(acbb);
        }

        protected class SampleRawDataReply : IRawDataReply
        {
            public Buffer Reply(PeerAddress sender, Buffer requestBuffer, bool complete)
            {
                // server returns just OK if same buffer is returned
                return requestBuffer;
            }
        }
    }
}
