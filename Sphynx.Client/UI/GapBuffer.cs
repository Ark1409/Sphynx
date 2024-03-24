using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Sphynx.Client.Utils;

namespace Sphynx.Client.UI
{
    /// <summary>
    /// Represents a buffer to which text can be arbitrarily inserted.
    /// </summary>
    public class GapBuffer<TChar> : IEnumerable<TChar>, IEquatable<IEnumerable<TChar>>, IEquatable<GapBuffer<TChar>?> where TChar : new()
    {
        /// <summary>
        /// Default size of the gap buffer
        /// </summary>
        public const int DEFAULT_GAP_SIZE = 16;

        /// <summary>
        /// Provides access to the raw buffer used internally by this gap buffer.
        /// </summary>
        public ReadOnlyCollection<TChar> Buffer => _buffer.AsReadOnly();

        /// <summary>
        /// Gets the number of characters in the full gap buffer.
        /// This is <i>NOT</i> the same as the number of characters in the text represented by this buffer.
        /// <seealso cref="Text"/>
        /// <seealso cref="Length"/>
        /// </summary>
        public int BufferCount => _buffer.Count;

        /// <summary>
        /// Gets the beginning index of the gap buffer.
        /// </summary>
        public int GapBegin { get; protected set; }

        /// <summary>
        /// Gets the index of the element one past the end of the gap buffer.
        /// </summary>
        public int GapEnd { get; protected set; }

        /// <summary>
        /// Gets the size of the gap within the gap buffer.
        /// </summary>
        public int GapSize => GapEnd - GapBegin;

        /// <summary>
        /// Gets the number of characters in the buffer's textual representation (with the gap omitted).
        /// </summary>
        public int Length => _buffer.Count - GapSize;

        /// <inheritdoc cref="Length"/>
        public int Count => Length;

        /// <summary>
        /// Converts the gap buffer into its textual representation, with the gap omitted.
        /// </summary>
        public virtual IEnumerable<TChar> Text
        {
            get
            {
                var textArray = new TChar[Length];

                var textSpan = textArray.AsSpan();
                var bufferSpan = CollectionsMarshal.AsSpan(_buffer);

                bufferSpan[..GapBegin].CopyTo(textSpan);
                bufferSpan[GapEnd..].CopyTo(textSpan[GapBegin..]);

                return textArray;
            }
        }

        protected readonly List<TChar> _buffer;

        /// <summary>
        /// Constructs a gap buffer with the specified initial gap size.
        /// </summary>
        /// <param name="initialCapacity">The initial size of the gap buffer. Defaults to <see cref="DEFAULT_GAP_SIZE"/>.</param>
        public GapBuffer(int initialCapacity = DEFAULT_GAP_SIZE)
        {
            initialCapacity = Math.Max(0, initialCapacity);
            _buffer = new List<TChar>(initialCapacity);
            _buffer.Resize(initialCapacity);
            GapBegin = 0;
            GapEnd = initialCapacity;
        }

        /// <summary>
        /// Constructs a gap buffer which initially holds the specified text.
        /// </summary>
        /// <param name="initialText">The text the gap buffer should initially hold.</param>
        /// <param name="initialCapacity">The initial size of the gap buffer. Must be at least <paramref name="initialText.Count()"/>.</param>
        public GapBuffer(IEnumerable<TChar> initialText, int initialCapacity) : this(Math.Max(initialText.Count(), initialCapacity)) { Insert(initialText); }

        /// <summary>
        /// Constructs a gap buffer which initially holds the specified text.
        /// Capacity defaults to
        /// <code>initialText.Count() + DEFAULT_GAP_SIZE</code>
        /// </summary>
        /// <param name="initialText">The text the gap buffer should initially hold.</param>
        public GapBuffer(IEnumerable<TChar> initialText) : this(initialText, initialText.Count() + DEFAULT_GAP_SIZE) { }

        /// <summary>
        /// Inserts text at the gap buffer's current position (i.e. <see cref="GapBegin"/>, relative to <see cref="Buffer"/>).
        /// The gap buffer is resized before the operation if the string to be inserted is larger than the current <see cref="GapSize"/>.
        /// </summary>
        /// <param name="text">The text to add to the buffer</param>
        /// <returns><c>this</c>.</returns>
        public virtual GapBuffer<TChar> Insert(IEnumerable<TChar> text)
        {
            var enumerable = text.ToArray();

            // Maybe come up with a better resizing function
            if (enumerable.Length > GapSize) GrowGap(enumerable.Length + _buffer.Count);

            enumerable.AsSpan().CopyTo(CollectionsMarshal.AsSpan(_buffer).Slice(GapBegin, enumerable.Length));

            GapBegin += enumerable.Length;
            return this;
        }

        /// <inheritdoc cref="GapBuffer{TChar}.Insert(IEnumerable{TChar})"/>
        public GapBuffer<TChar> Insert(TChar text) => Insert(new[] { text });

        /// <summary>
        /// Removes characters preceding the gap buffer
        /// </summary>
        /// <param name="count">The number of characters to remove.</param>
        /// <returns><c>this</c>.</returns>
        public GapBuffer<TChar> Erase(int count)
        {
            GapBegin = Math.Clamp(GapBegin - count, 0, GapEnd);
            return this;
        }

        /// <inheritdoc cref="Erase"/>
        public GapBuffer<TChar> Backspace(int count = 1) => Erase(count);

        /// <summary>
        /// Removes characters following the gap buffer.
        /// </summary>
        /// <param name="count">The number of characters to remove.</param>
        /// <returns><c>this</c>.</returns>
        public GapBuffer<TChar> Delete(int count)
        {
            GapEnd = Math.Clamp(GapEnd + count, GapBegin, _buffer.Count);
            return this;
        }

        /// <summary>
        /// Moves the gap buffer <paramref name="count"/> character(s) left or right.
        /// </summary>
        /// <param name="count">The amount by which the gap should be moved.
        /// This value is automatically clamped onto [-<see cref="GapBegin"/>, <see cref="BufferCount"/>-<see cref="GapEnd"/>].
        /// Negative values move left, positive values move right.</param>
        /// <returns><c>this</c>.</returns>
        public GapBuffer<TChar> Move(int count)
        {
            count = Math.Clamp(count, -GapBegin, _buffer.Count - GapEnd);

            var bufferSpan = CollectionsMarshal.AsSpan(_buffer);

            if (count >= 0)
            {
                bufferSpan.Slice(GapEnd, count).CopyTo(bufferSpan.Slice(GapBegin, count));
            }
            else
            {
                bufferSpan.Slice(GapBegin + count, -count).CopyTo(bufferSpan.Slice(GapEnd + count, -count));
            }

            GapBegin += count;
            GapEnd += count;
            return this;
        }

        /// <summary>
        /// Moves the gap buffer to the position specified by <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The position to which the gap buffer should be moved.
        /// This value is automatically clamped onto [0, <see cref="BufferCount"/>-<see cref="GapSize"/>].</param>
        /// <returns><c>this</c>.</returns>
        public GapBuffer<TChar> MoveAbs(int index) => Move(Math.Clamp(index, 0, Math.Max(0, _buffer.Count - GapSize)) - GapBegin);

        /// <summary>
        /// Resizes the gap to the specified number of characters
        /// </summary>
        /// <param name="gapSize">The new gap size</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="gapSize"/> &lt; 0.</exception>
        public void ResizeGap(int gapSize)
        {
            if (gapSize < 0) throw new ArgumentOutOfRangeException(nameof(gapSize), "Gap size cannot be negative");

            int copyCount = _buffer.Count - GapEnd;
            int gapDelta = gapSize - GapSize;

            if (gapDelta > 0) { _buffer.Grow(gapDelta); }

            var bufferSpan = CollectionsMarshal.AsSpan(_buffer);
            bufferSpan.Slice(GapEnd, copyCount).CopyTo(bufferSpan[(GapEnd + gapDelta)..]);

            if (gapDelta < 0) { _buffer.Shrink(-gapDelta); }

            GapEnd = GapBegin + gapSize;
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

        ///  <inheritdoc cref="Insert(IEnumerable{TChar})"/>
        public static GapBuffer<TChar> operator +(GapBuffer<TChar> b, IEnumerable<TChar> text) => b.Insert(text);

        ///  <inheritdoc cref="Insert(TChar)"/>
        public static GapBuffer<TChar> operator +(GapBuffer<TChar> b, TChar text) => b.Insert(text);

        ///  <inheritdoc cref="Move"/>
        public static GapBuffer<TChar> operator >> (GapBuffer<TChar> b, int count) => b.Move(count);

        ///  <inheritdoc cref="Move"/>
        public static GapBuffer<TChar> operator <<(GapBuffer<TChar> b, int count) => b >> (-count);

        /// <inheritdoc cref="Text"/>
        /// <returns><see cref="string"/> representation of gap buffer contents.</returns>
        public IEnumerator<TChar> GetEnumerator() => Text.GetEnumerator();

        /// <inheritdoc cref="GetEnumerator"/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(IEnumerable<TChar>? other) => other is not null && other.SequenceEqual(Text);

        public bool Equals(GapBuffer<TChar>? other) => other is not null && Equals(other.Text);

        public static bool operator ==(GapBuffer<TChar> instance, GapBuffer<TChar>? other) => instance.Equals(other);
        public static bool operator !=(GapBuffer<TChar> instance, GapBuffer<TChar>? other) => !(instance == other);

        public static bool operator ==(GapBuffer<TChar> instance, IEnumerable<TChar>? other) => instance.Equals(other);
        public static bool operator !=(GapBuffer<TChar> instance, IEnumerable<TChar>? other) => !(instance == other);
    }
}
