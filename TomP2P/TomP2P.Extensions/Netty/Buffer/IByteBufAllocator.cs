
namespace TomP2P.Extensions.Netty.Buffer
{
    public interface IByteBufAllocator
    {
        /// <summary>
        /// Allocate a ByteBuf with the given initial capacity and the given 
        /// maximal capacity.
        /// </summary>
        /// <param name="initialCapacity"></param>
        /// <param name="maxCapacity"></param>
        /// <returns></returns>
        ByteBuf Buffer(int initialCapacity, int maxCapacity);

        /// <summary>
        /// Allocate a direct ByteBuf with the given initial capacity.
        /// </summary>
        /// <param name="initialCapacity"></param>
        /// <returns></returns>
        ByteBuf DirectBuffer(int initialCapacity);

        /// <summary>
        /// Allocate a direct {@link ByteBuf} with the given initial capacity and 
        /// the given maximal capacity.
        /// </summary>
        /// <param name="initialCapacity"></param>
        /// <param name="maxCapacity"></param>
        /// <returns></returns>
        ByteBuf DirectBuffer(int initialCapacity, int maxCapacity);

        /// <summary>
        /// Allocate a heap ByteBuf with the fiven initial capacity.
        /// </summary>
        /// <param name="initialCapacity"></param>
        /// <returns></returns>
        ByteBuf HeapBuffer(int initialCapacity);

        ByteBuf HeapBuffer(int initialCapacity, int maxCapacity);
    }
}
