using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace TomP2P.Connection.Windows.Netty
{
    // TODO this pipeline can be optimized
    // - read/write do query the next handlers multiple times
    // - queries should be optimized
    // TODO add support for ExceptionCaught

    /// <summary>
    /// Equivalent to Java Netty's ChannelPipeline. Represents a chain of inbound and outbound handlers.
    /// Only the required parts are implemented.
    /// </summary>
    public class Pipeline
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IDictionary<string, HandlerItem> _name2Item;
        private readonly LinkedList<HandlerItem> _handlers;

        private readonly IChannel _channel;
        private readonly ChannelHandlerContext _ctx;
        private IOutboundHandler _currentOutbound = null;
        private IInboundHandler _currentInbound = null;

        private object _msgW;
        private object _msgR;
        private object _writeResult;
        private object _readResult;

        private Exception _caughtException;
        public bool IsActive { get; private set; }

        public Pipeline(IChannel channel)
            : this(channel, null)
        { }

        public Pipeline(IChannel channel, IEnumerable<KeyValuePair<string, IChannelHandler>> handlers)
        {
            _name2Item = new Dictionary<string, HandlerItem>();
            _handlers = new LinkedList<HandlerItem>();

            _channel = channel;
            _ctx = new ChannelHandlerContext(channel, this);

            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    AddLast(handler.Key, handler.Value);
                }
            }
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
                Logger.Debug("Channel '{0}': Processing outbound handler '{1}'.", _channel, _currentOutbound);
                if (_caughtException == null)
                {
                    _currentOutbound.Write(_ctx, msg);
                }
                else
                {
                    _currentOutbound.ExceptionCaught(_ctx, _caughtException);
                }
            }
            if (_writeResult == null)
            {
                _writeResult = _msgW;
            }
            return _writeResult;
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
                Logger.Debug("Channel '{0}': Processing inbound handler '{1}'.", _channel, _currentInbound);

                if (_caughtException == null)
                {
                    _currentInbound.Read(_ctx, msg);
                }
                else
                {
                    _currentInbound.ExceptionCaught(_ctx, _caughtException);
                }
            }
            if (_readResult == null)
            {
                _readResult = _msgR;
            }
            return _readResult;
        }

        public void ExceptionCaught(Exception ex)
        {
            _caughtException = ex;
        }

        public void Active()
        {
            IsActive = true;
            foreach (var handler in _handlers)
            {
                handler.Handler.ChannelActive(_ctx);
            }
        }

        public void Inactive()
        {
            IsActive = false;
            foreach (var handler in _handlers)
            {
                handler.Handler.ChannelInactive(_ctx);
            }
        }

        public void ResetWrite()
        {
            _currentOutbound = null;
            _msgW = null;
            _writeResult = null;
            _caughtException = null;
        }

        public void ResetRead()
        {
            _currentInbound = null;
            _msgR = null;
            _readResult = null;
            _caughtException = null;
        }

        private IOutboundHandler GetNextOutbound()
        {
            var outbounds = CurrentOutboundHandlers;
            if (outbounds.Count != 0)
            {
                if (_currentOutbound == null)
                {
                    _currentOutbound = outbounds.First.Value;
                }
                else
                {
                    var node = outbounds.Find(_currentOutbound);
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
            var inbounds = CurrentInboundHandlers;
            if (inbounds.Count != 0)
            {
                if (_currentInbound == null)
                {
                    _currentInbound = inbounds.First.Value;
                }
                else
                {
                    var node = inbounds.Find(_currentInbound);
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
            handler.HandlerAdded(_ctx);
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
            handler.HandlerAdded(_ctx);
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
            handler.HandlerAdded(_ctx);
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

            oldItem.Handler.HandlerRemoved(_ctx);
            oldItem.Name = newName;
            oldItem.Handler = newHandler;
            newHandler.HandlerAdded(_ctx);
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
                    item.Handler.HandlerRemoved(_ctx);
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

        private LinkedList<IOutboundHandler> CurrentOutboundHandlers
        {
            get
            {
                var outbounds = _handlers.
                    Select(item => item.Handler).
                    Where(handler => handler is IOutboundHandler).
                    Cast<IOutboundHandler>();
                return new LinkedList<IOutboundHandler>(outbounds);
            }
        }

        private LinkedList<IInboundHandler> CurrentInboundHandlers
        {
            get
            {
                var inbounds = _handlers.
                    Select(item => item.Handler).
                    Where(handler => handler is IInboundHandler).
                    Cast<IInboundHandler>();
                return new LinkedList<IInboundHandler>(inbounds);
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
    }
}
