using System.Runtime.InteropServices;

namespace Sphynx.Client.UI
{
    internal class StringGapBuffer : GapBuffer<char>
    {
        /// <inheritdoc/>
        public override string Text => string.Create<object?>(Length,
            null,
            (span, _) =>
            {
                var listSpan = CollectionsMarshal.AsSpan(_buffer);
                listSpan[..GapBegin].CopyTo(span);
                listSpan[GapEnd..].CopyTo(span[GapBegin..]);
            });

        /// <inheritdoc cref="GapBuffer{TChar}(int)"/>
        public StringGapBuffer(int initialCapacity = DEFAULT_GAP_SIZE) : base(initialCapacity) { }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar}, int)"/>
        public StringGapBuffer(IEnumerable<char> initialText, int initialCapacity) : base(initialText, initialCapacity) { }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar})"/>
        public StringGapBuffer(IEnumerable<char> initialText) : base(initialText) { }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar}, int)"/>
        public StringGapBuffer(string initialText, int initialCapacity) : this(Math.Max(initialText.Length, initialCapacity)) { Insert(initialText); }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar})"/>
        public StringGapBuffer(string initialText) : this(initialText, initialText.Length + DEFAULT_GAP_SIZE) { }

        /// <inheritdoc cref="GapBuffer{TChar}.Insert(IEnumerable{TChar})"/>
        public StringGapBuffer Insert(string text)
        {
            // Maybe come up with a better resizing function
            if (text.Length > GapSize) GrowGap(text.Length + _buffer.Count);

            text.AsSpan().CopyTo(CollectionsMarshal.AsSpan(_buffer).Slice(GapBegin, text.Length));

            GapBegin += text.Length;
            return this;
        }

        /// <inheritdoc cref="GapBuffer{TChar}.Insert(TChar)"/>
        public new StringGapBuffer Insert(char text) => Insert(new string(text, 1));

        /// <inheritdoc cref="GapBuffer{TChar}.Erase"/>
        public new StringGapBuffer Erase(int count) => (StringGapBuffer)base.Erase(count);

        /// <inheritdoc cref="GapBuffer{TChar}.Backspace"/>
        public new StringGapBuffer Backspace(int count = 1) => (StringGapBuffer)base.Backspace(count);

        /// <inheritdoc cref="GapBuffer{TChar}.Delete"/>
        public new StringGapBuffer Delete(int count) => (StringGapBuffer)base.Delete(count);

        /// <inheritdoc cref="GapBuffer{TChar}.Move(int)"/>
        public new StringGapBuffer Move(int count) => (StringGapBuffer)base.Move(count);

        /// <inheritdoc cref="GapBuffer{TChar}.MoveAbs(int)"/>
        public new StringGapBuffer MoveAbs(int index) => (StringGapBuffer)base.MoveAbs(index);
        
        /// <summary>
        /// Retrieves the (string) contents of the gap buffer.
        /// </summary>
        /// <returns>A <see cref="string"/> representation of the gap buffer's contents.</returns>
        /// <seealso cref="Text"/>
        public override string ToString() => Text;

        /// <inheritdoc cref="GapBuffer{TChar}.operator +(GapBuffer{TChar}, IEnumerable{TChar})"/>
        public static StringGapBuffer operator +(StringGapBuffer b, string text) => b.Insert(text);

        /// <inheritdoc cref="Text"/>
        /// <param name="b"><see cref="StringGapBuffer"/> instance</param>
        /// <returns><see cref="string"/> representation of gap buffer contents.</returns>
        public static explicit operator string(StringGapBuffer b) => b.Text;

        ///  <inheritdoc cref="GapBuffer{TChar}.operator +(GapBuffer{TChar}, IEnumerable{TChar})"/>
        public static StringGapBuffer operator +(StringGapBuffer b, IEnumerable<char> text) => (StringGapBuffer)((GapBuffer<char>)b + text);

        ///  <inheritdoc cref="GapBuffer{TChar}.operator +(GapBuffer{TChar}, TChar)"/>
        public static StringGapBuffer operator +(StringGapBuffer b, char text) => (StringGapBuffer)((GapBuffer<char>)b + text);

        ///  <inheritdoc cref="GapBuffer{TChar}.operator >>(GapBuffer{TChar}, int)"/>
        public static StringGapBuffer operator >> (StringGapBuffer b, int count) => (StringGapBuffer)((GapBuffer<char>)b >> count);

        ///  <inheritdoc cref="GapBuffer{TChar}.operator &lt;&lt;(GapBuffer{TChar}, int)"/>
        public static StringGapBuffer operator <<(StringGapBuffer b, int count) => (StringGapBuffer)((GapBuffer<char>)b << count);
    }
}
