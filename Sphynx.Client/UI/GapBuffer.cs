using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Sphynx.Client.Utils;

[assembly: InternalsVisibleTo("Sphynx.Test")]

namespace Sphynx.Client.UI
{
    /// <summary>
    /// Represents a buffer to which text can be arbitrarily inserted.
    /// </summary>
    internal class GapBuffer
    {
        /// <summary>
        /// Default size of the gap buffer
        /// </summary>
        public const int DEFAULT_GAP_SIZE = 16;

        /// <summary>
        /// Provides access to the raw buffer used internally by this gap buffer.
        /// </summary>
        public ReadOnlyCollection<char> Buffer => _buffer.AsReadOnly();

        /// <summary>
        /// Gets the number of characters in the full gap buffer.
        /// This is <i>NOT</i> the same as the number of characters in the text represented by this buffer.
        /// <seealso cref="Text"/>
        /// </summary>
        public int Count => _buffer.Count;

        /// <summary>
        /// Gets the beginning index of the gap buffer.
        /// </summary>
        public int GapBegin { get; private set; }

        /// <summary>
        /// Gets the end index of the gap buffer.
        /// </summary>
        public int GapEnd { get; private set; }

        /// <summary>
        /// Gets the size of the gap within the gap buffer.
        /// </summary>
        public int GapSize => GapEnd - GapBegin + 1;

        /// <summary>
        /// Converts the gap buffer into its textual representation, with the gap omitted.
        /// </summary>
        public string Text
        {
            get
            {
                var sb = new StringBuilder(GapBegin + _buffer.Count - GapEnd);
                var listSpan = CollectionsMarshal.AsSpan(_buffer);
                foreach (char c in listSpan[..GapBegin])
                {
                    sb.Append(c);
                }

                foreach (char c in listSpan[(GapEnd + 1)..])
                {
                    sb.Append(c);
                }
                return sb.ToString();
            }
        }

        private readonly List<char> _buffer;

        /// <summary>
        /// Constructs a gap buffer with the specified initial gap size.
        /// </summary>
        /// <param name="initialCapacity">The initial size of the gap buffer. Defaults to <see cref="DEFAULT_GAP_SIZE"/>.</param>
        public GapBuffer(int initialCapacity = DEFAULT_GAP_SIZE)
        {
            initialCapacity = Math.Max(1, initialCapacity);
            _buffer = new List<char>(initialCapacity);
            _buffer.Resize(initialCapacity);
            GapBegin = 0;
            GapEnd = initialCapacity - 1;
        }

        /// <summary>
        /// Constructs a gap buffer which initially holds the specified text.
        /// </summary>
        /// <param name="initialText">The text the gap buffer should initially hold.</param>
        /// <param name="initialCapacity">The initial size of the gap buffer. Must be at least <paramref name="initialText.Length"/>.</param>
        public GapBuffer(string initialText, int initialCapacity) : this(Math.Max(initialText.Length, initialCapacity)) { Insert(initialText); }

        /// <summary>
        /// Constructs a gap buffer which initially holds the specified text.
        /// Capacity defaults to
        /// <code>initialText.Length * 2 + DEFAULT_GAP_SIZE * 2</code>
        /// </summary>
        /// <param name="initialText">The text the gap buffer should initially hold.</param>
        public GapBuffer(string initialText) : this(initialText, initialText.Length * 2 + DEFAULT_GAP_SIZE * 2) { }

        /// <summary>
        /// Inserts text at the gap buffer's current position (i.e. <see cref="GapBegin"/>, relative to <see cref="Buffer"/>).
        /// The gap buffer is resized before the operation if the string to be inserted is larger than the current <see cref="GapSize"/>.
        /// </summary>
        /// <param name="str">The text to add to the buffer</param>
        /// <returns><c>this</c>.</returns>
        public GapBuffer Insert(string str)
        {
            // Maybe come up with a better resizing function
            if (str.Length > GapSize) GrowGap(str.Length + DEFAULT_GAP_SIZE * 2);

            unsafe
            {
                fixed (char* bufferPtr = CollectionsMarshal.AsSpan(_buffer))
                fixed (char* strPtr = str)
                {
                    System.Buffer.MemoryCopy(strPtr, bufferPtr + GapBegin, str.Length * sizeof(char), str.Length * sizeof(char));
                }
            }

            GapBegin += str.Length;
            return this;
        }

        /// <summary>
        /// Moves the gap buffer <paramref name="count"/> character(s) left or right.
        /// </summary>
        /// <param name="count">The amount by which the gap should be moved.
        /// This value is automatically clamped onto [-<see cref="GapBegin"/>, <see cref="Count"/>-<see cref="GapEnd"/>).
        /// Negative values move left, positive values move right.</param>
        /// <returns><c>this</c>.</returns>
        public unsafe GapBuffer Move(int count)
        {
            count = Math.Clamp(count, -GapBegin, _buffer.Count - GapEnd - 1);

            fixed (char* ptr = CollectionsMarshal.AsSpan(_buffer))
            {
                if (count > 0) { System.Buffer.MemoryCopy(ptr + GapEnd + 1, ptr + GapBegin, count * sizeof(char), count * sizeof(char)); }
                else { System.Buffer.MemoryCopy(ptr + GapBegin + count, ptr + GapEnd + 1 + count, -count * sizeof(char), -count * sizeof(char)); }
            }

            GapBegin += count;
            GapEnd += count;
            return this;
        }

        /// <summary>
        /// Moves the gap buffer to the position specified by <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The position to which the gap buffer should be moved.
        /// This value is automatically clamped onto [0, <see cref="Count"/>-<see cref="GapEnd"/>).</param>
        /// <returns><c>this</c>.</returns>
        public GapBuffer MoveAbs(int index) => Move(-(GapBegin - Math.Clamp(index, 0, Math.Max(0, _buffer.Count - GapEnd - 1))));

        /// <summary>
        /// Resizes the gap to the specified number of characters
        /// </summary>
        /// <param name="gapSize">The new gap size</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="gapSize"/> &lt; 0.</exception>
        public unsafe void ResizeGap(int gapSize)
        {
            if (gapSize < 0) throw new ArgumentOutOfRangeException(nameof(gapSize), "Gap size must be greater than 0");

            int copyCount = _buffer.Count - GapEnd - 1;

            if (gapSize > GapSize)
            {
                int gapDelta = gapSize - GapSize;
                _buffer.Grow(Math.Abs(gapDelta));

                if (copyCount > 0)
                {
                    fixed (char* ptr = CollectionsMarshal.AsSpan(_buffer))
                    {
                        System.Buffer.MemoryCopy(ptr + GapEnd + 1, ptr + GapEnd + 1 + gapDelta, copyCount * sizeof(char), copyCount * sizeof(char));
                    }
                }
            }
            else
            {
                int gapDelta = GapSize - gapSize;
                if (copyCount > 0)
                {
                    fixed (char* ptr = CollectionsMarshal.AsSpan(_buffer))
                    {
                        System.Buffer.MemoryCopy(ptr + GapEnd + 1, ptr + GapBegin + gapSize, copyCount * sizeof(char), copyCount * sizeof(char));
                    }
                }
                _buffer.Shrink(Math.Abs(gapDelta));
            }

            GapEnd = GapBegin + gapSize - 1;
        }

        /// <summary>
        /// Grows the gap <paramref name="count"/> amount of characters
        /// </summary>
        /// <param name="count">The number of characters to grow by.</param>
        public void GrowGap(int count) => ResizeGap(GapSize + Math.Max(-GapSize, count));

        /// <summary>
        /// Shrinks the gap <paramref name="count"/> amount of characters
        /// </summary>
        /// <param name="count">The number of characters to shrink by.</param>
        public void ShrinkGap(int count) => ResizeGap(GapSize - Math.Min(GapSize, count));

        /// <inheritdoc/>
        public override string ToString() => Text;

        ///  <inheritdoc cref="Insert"/>
        public static GapBuffer operator +(GapBuffer b, string str) => b.Insert(str);

        ///  <inheritdoc cref="Move"/>
        public static GapBuffer operator +(GapBuffer b, int count) => b.Move(count);

        ///  <inheritdoc cref="Move"/>
        public static GapBuffer operator -(GapBuffer b, int count) => b + (-count);

        /// <inheritdoc cref="Text"/>
        /// <param name="b"><see cref="GapBuffer"/> instance</param>
        /// <returns><see cref="string"/> representation of gap buffer contents.</returns>
        public static explicit operator string(GapBuffer b) => b.Text;
    }
}
