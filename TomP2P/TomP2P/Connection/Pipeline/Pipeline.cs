using System;
using System.Collections.Generic;

namespace TomP2P.Connection.Pipeline
{
    /// <summary>
    /// A lightweight .NET equivalent for the Java Netty ChannelPipeline.
    /// </summary>
    public class Pipeline
    {
        // TODO use ChannelHandlerContext instead of object
        private readonly IDictionary<string, Handler> _name2ctx = new Dictionary<string, Handler>();

        public Pipeline AddLast(string name, Handler handler)
        {
            lock (this)
            {
                CheckDuplicateName(name);
                _name2ctx.Add(name, handler);
            }
            return this;
        }

        private void CheckDuplicateName(string name)
        {
            if (_name2ctx.ContainsKey(name))
            {
                throw new ArgumentException("Duplicate handler name {0}.", name);
            }
        }
    }
}
