namespace TomP2P.Core.Connection.Windows.Netty
{
    /// <summary>
    /// Equivalent to Java Netty's @Sharable annotation.
    /// Indicates that the same instance of the annotated IChannelHandler can be added to one or more Pipelines multiple times without a race condition. 
    /// If this annotation is not specified, you have to create a new handler instance every time you add it to a pipeline because it has unshared state such as member variables. 
    /// </summary>
    public interface ISharable
    { }
}
