namespace TomP2P.Core.Futures
{
    /// <summary>
    /// A cancelable class should implement this method and use if for future objects.
    /// </summary>
    public interface ICancel
    {
        /// <summary>
        /// Gets called if a future is cancelled.
        /// </summary>
        void Cancel();
    }
}
