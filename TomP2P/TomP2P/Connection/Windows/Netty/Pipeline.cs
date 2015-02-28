using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace TomP2P.Connection.Windows.Netty
{
    // TODO this pipeline can be optimized
    // - read/write do query the next handlers multiple times
    // - queries should be optimized
    // - use re-usable sessions

    /// <summary>
    /// Equivalent to Java Netty's ChannelPipeline. Represents a chain of inbound and outbound handlers.
    /// Only the required parts are implemented.
    /// </summary>
    public class Pipeline
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IChannel _channel;

        private readonly IDictionary<string, HandlerItem> _name2Item;
        private readonly LinkedList<HandlerItem> _handlers;

        public Pipeline(IChannel channel)
            : this(channel, null)
        { }

        public Pipeline(IChannel channel, IEnumerable<KeyValuePair<string, IChannelHandler>> handlers)
        {
            _channel = channel;
            _name2Item = new Dictionary<string, HandlerItem>();
            _handlers = new LinkedList<HandlerItem>();

            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    AddLast(handler.Key, handler.Value);
                }
            }
        }

        internal PipelineSession GetNewSession()
        {
            Logger.Debug("Creating session for channel {0}.", Channel);
            // for each non-sharable handler, a new instance has to be created
            var newInbounds = CreateNewInstances(InboundHandlers);
            var newOutbounds = CreateNewInstances(OutboundHandlers);

            var session = new PipelineSession(this, newInbounds.Cast<IInboundHandler>(), newOutbounds.Cast<IOutboundHandler>());
            
            // activate channel
            var handlers = newInbounds.Union(newOutbounds);
            foreach (var item in handlers)
            {
                item.ChannelActive(session.ChannelHandlerContext);
            }

            return session;
        }

        internal void ReleaseSession(PipelineSession session)
        {
            // inactivate channel
            var handlers = session.InboundHandlers.Cast<IChannelHandler>()
                .Union(session.OutboundHandlers);
            foreach (var item in handlers)
            {
                item.ChannelInactive(session.ChannelHandlerContext);
            }
        }

        /// <summary>
        /// Inserts a handler at the first position of this pipeline.
        /// </summary>
        /// <returns></returns>
        public Pipeline AddFirst(string name, IChannelHandler handler)
        {
            // TODO check if works correctly!
            CheckDuplicateName(name);
            var item = new HandlerItem(name, handler);
            _name2Item.Add(name, item);

            _handlers.AddFirst(item);
            //handler.HandlerAdded(_ctx);
            return this;
        }

        /// <summary>
        /// Appends a handler at the last position of this pipeline.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Pipeline AddLast(string name, IChannelHandler handler)
        {
            CheckDuplicateName(name);
            var item = new HandlerItem(name, handler);
            _name2Item.Add(name, item);

            _handlers.AddLast(item);
            //handler.HandlerAdded(_ctx);
            return this;
        }

        /// <summary>
        /// Inserts a handler before an existing handler of this pipeline.
        /// </summary>
        /// <param name="baseName"></param>
        /// <param name="name"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Pipeline AddBefore(string baseName, string name, IChannelHandler handler)
        {
            // TODO check if works correctly!
            HandlerItem baseItem;
            if (!_name2Item.TryGetValue(baseName, out baseItem))
            {
                throw new ArgumentException("The requested base item does not exist in this pipeline.");
            }

            CheckDuplicateName(name);
            var item = new HandlerItem(name, handler);
            _name2Item.Add(name, item);

            var nBase = new LinkedListNode<HandlerItem>(baseItem);
            var nNew = new LinkedListNode<HandlerItem>(item);

            _handlers.AddBefore(nBase, nNew);
            //handler.HandlerAdded(_ctx);
            return this;
        }

        /// <summary>
        /// Replaces the specified handler with a new handler in this pipeline.
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        /// <param name="newHandler"></param>
        /// <returns></returns>
        public Pipeline Replace(string oldName, string newName, IChannelHandler newHandler)
        {
            // TODO check if works correctly!
            HandlerItem oldItem;
            if (!_name2Item.TryGetValue(oldName, out oldItem))
            {
                throw new ArgumentException("The requested base item does not exist in this pipeline.");
            }
            CheckDuplicateName(newName);

            //oldItem.Handler.HandlerRemoved(_ctx);
            oldItem.Name = newName;
            oldItem.Handler = newHandler;
            //newHandler.HandlerAdded(_ctx);
            return this;
        }

        /// <summary>
        /// Removes the channel handler with the specified name from this pipeline.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Pipeline Remove(string name)
        {
            // TODO works?
            if (_name2Item.ContainsKey(name))
            {
                var item = _name2Item[name];
                _name2Item.Remove(name);

                if (_handlers.Contains(item))
                {
                    _handlers.Remove(item);
                    //item.Handler.HandlerRemoved(_ctx);
                }
            }
            return this;
        }

        private void CheckDuplicateName(string name)
        {
            if (_name2Item.ContainsKey(name))
            {
                throw new ArgumentException("Duplicate handler name: " + name);
            }
        }

        private LinkedList<IChannelHandler> CreateNewInstances(IEnumerable<IChannelHandler> oldHandlers)
        {
            var newHandlers = new LinkedList<IChannelHandler>();
            foreach (var handler in oldHandlers)
            {
                if (handler is ISharable)
                {
                    // add same handler (shared reference)
                    newHandlers.AddLast(handler);
                }
                else
                {
                    // add new, cloned handler (not same reference)
                    newHandlers.AddLast(handler.CreateNewInstance());
                }
            }
            return newHandlers;
        }

        private IEnumerable<IOutboundHandler> OutboundHandlers
        {
            get
            {
                var outbounds = _handlers.
                    Select(item => item.Handler).
                    Where(handler => handler is IOutboundHandler).
                    Cast<IOutboundHandler>();
                return new List<IOutboundHandler>(outbounds);
            }
        }

        private IEnumerable<IInboundHandler> InboundHandlers
        {
            get
            {
                var inbounds = _handlers.
                    Select(item => item.Handler).
                    Where(handler => handler is IInboundHandler).
                    Cast<IInboundHandler>();
                return new List<IInboundHandler>(inbounds);
            }
        }

        public IChannel Channel
        {
            get { return _channel; }
        }

        /// <summary>
        /// Returns the list of handler names.
        /// </summary>
        public IList<string> Names
        {
            get { return _handlers.Select(hi => hi.Name).ToList(); }
        }

        private struct HandlerItem
        {
            public string Name { get; set; }
            public IChannelHandler Handler { get; set; }

            public HandlerItem(string name, IChannelHandler handler) : this()
            {
                Name = name;
                Handler = handler;
            }
        }

        /// <summary>
        /// Wraps the internal state of a pipeline session. This is necessary because multiple
        /// pipeline sessions can run in parallel, especially on the server-side.
        /// </summary>
        public class PipelineSession
        {
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

            public PipelineSession(Pipeline pipeline, IEnumerable<IInboundHandler> inboundHandlers,
                IEnumerable<IOutboundHandler> outboundHandlers)
            {
                _pipeline = pipeline;
                InboundHandlers = new LinkedList<IInboundHandler>(inboundHandlers);
                OutboundHandlers = new LinkedList<IOutboundHandler>(outboundHandlers);
                _ctx = new ChannelHandlerContext(this);
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
                    Logger.Debug("Channel '{0}': Processing outbound handler '{1}'.", _pipeline.Channel, _currentOutbound);
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
                    Logger.Debug("Channel '{0}': Processing inbound handler '{1}'.", _pipeline.Channel, _currentInbound);

                    if (_caughtException != null)
                    {
                        _currentInbound.ExceptionCaught(_ctx, _caughtException);
                    }
                    else if (_event != null)
                    {
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
                return _readRes;
            }

            public void ExceptionCaught(Exception ex)
            {
               _caughtException = ex;
            }

            public void UserEventTriggered(object evt)
            {
                _event = evt;
            }

            /*public void ResetWrite()
            {
                CurrentOutbound = null;
                MsgW = null;
                WriteRes = null;
                CaughtException = null;
                Event = null;
            }

            public void ResetRead()
            {
                CurrentInbound = null;
                MsgR = null;
                ReadRes = null;
                CaughtException = null;
                Event = null;
            }*/

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

            public ChannelHandlerContext ChannelHandlerContext
            {
                get { return _ctx; }
            }
        }
    }
}
