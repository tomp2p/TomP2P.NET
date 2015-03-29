using System;
using System.Threading.Tasks;
using TomP2P.Core.Connection;
using TomP2P.Core.P2P;
using TomP2P.Core.P2P.Builder;
using TomP2P.Core.Peers;
using TomP2P.Core.Rpc;
using TomP2P.Core.Storage;
using Buffer = TomP2P.Core.Message.Buffer;

namespace TomP2P.Benchmark
{
    public class SendDirectLocalProfiler : Profiler
    {
        private const int NetworkSize = 2;
        private readonly bool _isForceUdp;
        private Peer _sender;
        private Peer _receiver;
        private ChannelCreator _cc;
        private SendDirectBuilder _sendDirectBuilder;

        public SendDirectLocalProfiler(bool isForceUdp)
        {
            _isForceUdp = isForceUdp;
        }

        protected override async Task SetupAsync()
        {
            Network = BenchmarkUtil.CreateNodes(NetworkSize, Rnd, 7077, false, false);
            _sender = Network[0];
            _receiver = Network[1];
            _receiver.RawDataReply(new SampleRawDataReply());
            _cc = await _sender.ConnectionBean.Reservation.CreateAsync(_isForceUdp ? 1 : 0, _isForceUdp ? 0 : 1);

            _sendDirectBuilder = new SendDirectBuilder(_sender, (PeerAddress) null)
                .SetIsStreaming()
                .SetIdleUdpSeconds(0)
                .SetIdleTcpSeconds(0)
                .SetBuffer(CreateSampleBuffer())
                .SetIsForceUdp(_isForceUdp); // TODO check if works
        }

        protected override async Task ShutdownAsync()
        {
            if (_sender != null)
            {
                await _sender.ShutdownAsync();
            }
            if (_receiver != null)
            {
                await _receiver.ShutdownAsync();
            }
            if (_cc != null)
            {
                await _cc.ShutdownAsync();
            }
        }

        protected override async Task ExecuteAsync()
        {
            await _sender.DirectDataRpc.SendAsync(_receiver.PeerAddress, _sendDirectBuilder, _cc);
            
            // make buffer reusable
            _sendDirectBuilder.Buffer.Reset();
            _sendDirectBuilder.Buffer.BackingBuffer.SetReaderIndex(0);
        }

        private static Buffer CreateSampleBuffer()
        {
            var acbb = AlternativeCompositeByteBuf.CompBuffer();
            acbb.WriteBytes(new sbyte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9});
            return new Buffer(acbb);
        }

        private class SampleRawDataReply : IRawDataReply
        {
            public Buffer Reply(PeerAddress sender, Buffer requestBuffer, bool complete)
            {
                return requestBuffer;
            }
        }
    }
}
