using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NLog;

namespace TomP2P.Core.Connection.Windows.Netty
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

        private readonly IDictionary<string, HandlerItem> _name2Item;
        private readonly LinkedList<HandlerItem> _handlers;

        public Pipeline(IEnumerable<KeyValuePair<string, IChannelHandler>> handlers = null)
        {
            _name2Item = new Dictionary<string, HandlerItem>();
            _handlers = new LinkedList<HandlerItem>();

            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    AddLast(handler.Key, handler.Value);
                }
            }
            Logger.Info("Instantiated {0}.", this);
        }

        /// <summary>
        /// Creates a new <see cref="PipelineSession"/> for the client-side pipeline.
        /// All handlers are re-used.
        /// </summary>
        /// <returns></returns>
        public PipelineSession CreateClientSession(IClientChannel clientChannel)
        {
            Logger.Debug("Creating session for {0}.", clientChannel);
            return new PipelineSession(clientChannel, this, InboundHandlers, OutboundHandlers);
        }

        /// <summary>
        /// Creates a new <see cref="PipelineSession"/> for the server-side pipeline.
        /// Sharable handlers are re-used, non-shareable handlers are cloned.
        /// All handlers are notified about activity.
        /// </summary>
        /// <returns></returns>
        public PipelineSession CreateNewServerSession(IServerChannel serverChannel)
        {
            Logger.Debug("Creating session for {0}.", serverChannel);
            // for each non-sharable handler, a new instance has to be created
            var newInbounds = CreateNewInstances(InboundHandlers, serverChannel).Cast<IInboundHandler>();
            var newOutbounds = CreateNewInstances(OutboundHandlers, serverChannel).Cast<IOutboundHandler>();

            return new PipelineSession(serverChannel, this, newInbounds, newOutbounds);
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

        private IEnumerable<IChannelHandler> CreateNewInstances(IEnumerable<IChannelHandler> oldHandlers, IChannel channel)
        {
            var newHandlers = new List<IChannelHandler>();
            foreach (var handler in oldHandlers)
            {
                if (handler is ISharable)
                {
                    // add same handler (shared reference)
                    newHandlers.Add(handler);
                    Logger.Info("{0}: Sharing handler {1} for {2}.", this, handler, channel);
                }
                else
                {
                    // add new, cloned handler (not same reference)
                    var newHandler = handler.CreateNewInstance();
                    newHandlers.Add(newHandler);
                    Logger.Info("{0}: Created new handler {1} for {2}.", this, newHandler, channel);
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

        /// <summary>
        /// Returns the list of handler names.
        /// </summary>
        public IList<string> Names
        {
            get { return _handlers.Select(hi => hi.Name).ToList(); }
        }

        /// <summary>
        /// Returns the list of handlers.
        /// </summary>
        public IList<IChannelHandler> Handlers
        {
            get { return _handlers.Select(hi => hi.Handler).ToList(); }
        }

        public LinkedList<HandlerItem> HandlerItems
        {
            get { return _handlers; }
        }

        public override string ToString()
        {
            return String.Format("Pipeline ({0})", RuntimeHelpers.GetHashCode(this));
        }

        public struct HandlerItem
        {
            public string Name { get; set; }
            public IChannelHandler Handler { get; set; }

            public HandlerItem(string name, IChannelHandler handler)
                : this()
            {
                Name = name;
                Handler = handler;
            }
        }
    }
}
