
namespace TomP2P.Extensions.Netty
{
    public sealed class ByteBufUtil
    {
        /// <summary>
        /// Returns true if and only if the two specified buffers are identical to each 
        /// other. This method is useful when implementing a new buffer type.
        /// </summary>
        /// <param name="bufferA"></param>
        /// <param name="bufferB"></param>
        /// <returns></returns>
        public static bool Equals(ByteBuf bufferA, ByteBuf bufferB)
        {
            int aLen = bufferA.ReadableBytes;
            if (aLen != bufferB.ReadableBytes) {
                return false;
            }

            int longCount = aLen >> 3;
            int byteCount = aLen & 7;

            int aIndex = bufferA.ReaderIndex;
            int bIndex = bufferB.ReaderIndex;

            //if (bufferA.order() == bufferB.order()) {
            for (int i = longCount; i > 0; i --)
            {
                if (bufferA.GetLong(aIndex) != bufferB.GetLong(bIndex))
                {
                    return false;
                }
                aIndex += 8;
                bIndex += 8;
            }
            /*} else {
                for (int i = longCount; i > 0; i --) {
                    if (bufferA.getLong(aIndex) != swapLong(bufferB.getLong(bIndex))) {
                        return false;
                    }
                    aIndex += 8;
                    bIndex += 8;
                }
            }*/

            for (int i = byteCount; i > 0; i --)
            {
                if (bufferA.GetByte(aIndex) != bufferB.GetByte(bIndex))
                {
                    return false;
                }
                aIndex ++;
                bIndex ++;
            }

            return true;
        }

        /// <summary>
        /// Calculates the hash code of the specified buffer.
        /// This method is useful when implementing a new buffer type.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static int HashCode(ByteBuf buffer)
        {
            int aLen = buffer.ReadableBytes;
            int intCount = aLen >> 2;
            int byteCount = aLen & 3;

            int hashCode = 1;
            int arrayIndex = buffer.ReaderIndex;
            //if (buffer.order() == ByteOrder.BIG_ENDIAN) {
            for (int i = intCount; i > 0; i --)
            {
                hashCode = 31 * hashCode + buffer.GetInt(arrayIndex);
                arrayIndex += 4;
            }
            /*} else {
                for (int i = intCount; i > 0; i --) {
                    hashCode = 31 * hashCode + swapInt(buffer.getInt(arrayIndex));
                    arrayIndex += 4;
                }
            }*/

            for (int i = byteCount; i > 0; i --)
            {
                hashCode = 31 * hashCode + buffer.GetByte(arrayIndex ++);
            }

            if (hashCode == 0)
            {
                hashCode = 1;
            }

            return hashCode;
        }
    }
}
