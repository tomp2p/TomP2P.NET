using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace TomP2P.Connection.Windows.Netty
{
    /// <summary>
    /// Wraps the internal state of a pipeline session. This is necessary because multiple
    /// pipeline sessions can run in parallel, especially on the server-side.
    /// </summary>
    public class PipelineSession
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private volatile bool _isTimedOut;
        public bool IsTimedOut { get { return _isTimedOut; } }

        private readonly IChannel _channel;
        private readonly Pipeline _pipeline;
        public LinkedList<IInboundHandler> InboundHandlers { get; private set; }
        public LinkedList<IOutboundHandler> OutboundHandlers { get; private set; }
        private readonly ChannelHandlerContext _ctx;

        private IOutboundHandler _currentOutbound;
        private IInboundHandler _currentInbound;
        private object _msgW;
        private object _msgR;
        private object _writeRes;
        private object _readRes;

        private Exception _caughtException;
        private object _event;

        public PipelineSession(IChannel channel, Pipeline pipeline, IEnumerable<IInboundHandler> inboundHandlers,
            IEnumerable<IOutboundHandler> outboundHandlers)
        {
            _channel = channel;
            _pipeline = pipeline;
            InboundHandlers = new LinkedList<IInboundHandler>(inboundHandlers);
            OutboundHandlers = new LinkedList<IOutboundHandler>(outboundHandlers);
            _ctx = new ChannelHandlerContext(channel, this);
        }

        public void Reset()
        {
            _currentOutbound = null;
            _currentInbound = null;
            _msgW = null;
            _msgR = null;
            _writeRes = null;
            _readRes = null;
            _caughtException = null;
            _event = null;
        }

        public void TriggerActive()
        {
            var handlers = InboundHandlers.Cast<IChannelHandler>().Union(OutboundHandlers);
            foreach (var item in handlers.Where(item => !item.IsActivated))
            {
                item.ChannelActive(_ctx);
            }
        }

        public void TriggerInactive()
        {
            var handlers = InboundHandlers.Cast<IChannelHandler>().Union(OutboundHandlers);
            foreach (var item in handlers.Where(item => item.IsActivated))
            {
                item.ChannelInactive(_ctx);
            }
        }

        public void TriggerTimeout()
        {
            _isTimedOut = true;
        }

        public object Write(object msg)
        {
            if (msg == null)
            {
                throw new NullReferenceException("msg");
            }
            // set msgW to newest provided value
            _msgW = msg;

            // find next outbound handler
            while (GetNextOutbound() != null)
            {
                Logger.Debug("{0}: Processing outbound handler {1}.", _pipeline, _currentOutbound);
                if (_caughtException != null)
                {
                    _currentOutbound.ExceptionCaught(_ctx, _caughtException);
                }
                else
                {
                    _currentOutbound.Write(_ctx, msg);
                }
            }
            if (_writeRes == null)
            {
                _writeRes = _msgW;
            }
            if (_caughtException != null)
            {
                Logger.Error("Exception in outbound pipeline: {0}", _caughtException.ToString());
                throw _caughtException;
            }
            return _writeRes;
        }

        public object Read(object msg)
        {
            if (msg == null)
            {
                throw new NullReferenceException("msg");
            }

            // set msgR to newest provided value
            _msgR = msg;

            // find next inbound handler
            while (GetNextInbound() != null)
            {
                Logger.Debug("{0}: Processing inbound handler {1}.", _pipeline, _currentInbound);

                if (_caughtException != null)
                {
                    _currentInbound.ExceptionCaught(_ctx, _caughtException);
                }
                else if (_event != null)
                {
                    // TODO check if this is correct
                    _currentInbound.UserEventTriggered(_ctx, _event);
                }
                else
                {
                    _currentInbound.Read(_ctx, msg);
                }
            }
            if (_readRes == null)
            {
                _readRes = _msgR;
            }
            if (_caughtException != null)
            {
                Logger.Error("Exception in inbound pipeline: {0}", _caughtException.ToString());
                throw _caughtException;
            }
            return _readRes;
        }

        public void ExceptionCaught(Exception ex)
        {
            _caughtException = ex;
        }

        public void UserEventTriggered(object evt)
        {
            // iterate through all inbound handlers
            _event = evt;
            foreach (var inboundHandler in InboundHandlers)
            {
                inboundHandler.UserEventTriggered(_ctx, evt);
            }
        }

        private IOutboundHandler GetNextOutbound()
        {
            if (OutboundHandlers.Count != 0)
            {
                if (_currentOutbound == null)
                {
                    _currentOutbound = OutboundHandlers.First.Value;
                }
                else
                {
                    var node = OutboundHandlers.Find(_currentOutbound);
                    if (node != null && node.Next != null)
                    {
                        _currentOutbound = node.Next.Value;
                        return _currentOutbound;
                    }
                    return null;
                }
                return _currentOutbound;
            }
            return null;
        }

        private IInboundHandler GetNextInbound()
        {
            if (InboundHandlers.Count != 0)
            {
                if (_currentInbound == null)
                {
                    _currentInbound = InboundHandlers.First.Value;
                }
                else
                {
                    var node = InboundHandlers.Find(_currentInbound);
                    if (node != null && node.Next != null)
                    {
                        _currentInbound = node.Next.Value;
                        return _currentInbound;
                    }
                    return null;
                }
                return _currentInbound;
            }
            return null;
        }

        public Pipeline Pipeline
        {
            get { return _pipeline; }
        }

        public IChannel Channel
        {
            get { return _channel; }
        }

        public ChannelHandlerContext ChannelHandlerContext
        {
            get { return _ctx; }
        }
    }
}
