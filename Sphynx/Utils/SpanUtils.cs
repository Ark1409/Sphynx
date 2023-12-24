namespace Sphynx.Utils
{
    internal static class SpanUtils
    {
        internal unsafe static void CopyFrom<T>(this Span<T> span, int spanOffset, T[] buffer, int bufferOffset) where T : unmanaged
        {
            fixed (T* bufferPtr = buffer)
            {
                CopyFrom(span, spanOffset, bufferPtr, bufferOffset, buffer.Length);
            }
        }

        internal unsafe static void CopyFrom<T>(this Span<T> span, int spanOffset, T* buffer, int bufferOffset, int bufferLength) where T : unmanaged
        {
            if (span.Length - spanOffset < bufferLength - bufferOffset)
                throw new ArgumentException("Buffer too large", nameof(buffer));

            for (int i = 0; i < bufferLength - bufferOffset; i++)
            {
                span[i + spanOffset] = buffer[i + bufferOffset];
            }
        }

        internal unsafe static void CopyFrom<T>(this Span<T> span, T* buffer, int bufferLength) where T : unmanaged => CopyFrom(span, 0, buffer, 0, bufferLength);

        internal static void CopyFrom<T>(this Span<T> span, T[] buffer) where T : unmanaged => CopyFrom(span, 0, buffer, 0);
    }
}
