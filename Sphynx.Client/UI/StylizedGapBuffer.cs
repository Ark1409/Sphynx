using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Spectre.Console;

namespace Sphynx.Client.UI
{
    public class StylizedGapBuffer : GapBuffer<ValueTuple<char, Style>>
    {
        public override ValueTuple<char, Style>[] Text
        {
            get
            {
                var textArray = new ValueTuple<char, Style>[Length];

                var textSpan = textArray.AsSpan();
                var bufferSpan = CollectionsMarshal.AsSpan(_buffer);

                bufferSpan[..GapBegin].CopyTo(textSpan);
                bufferSpan[GapEnd..].CopyTo(textSpan[GapBegin..]);

                return textArray;
            }
        }


        public string PlainText => string.Create<object?>(Length,
            null,
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            (span, _) =>
            {
                var bufferSpan = CollectionsMarshal.AsSpan(_buffer);

                for (int i = 0; i < GapBegin; i++)
                {
                    span[i] = bufferSpan[i].Item1;
                }

                for (int i = GapEnd; i < BufferCount; i++)
                {
                    span[i - GapSize] = bufferSpan[i].Item1;
                }
            });

        public IList<ValueTuple<string, Style>> TextBlocks
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            get
            {
                var list = new List<ValueTuple<string, Style>>();
                if (Length <= 0) return list;

                var sb = new StringBuilder(Length);
                var bufferSpan = CollectionsMarshal.AsSpan(_buffer);

                Style? currentStyle = null;
                for (int i = 0; i < BufferCount; i++)
                {
                    if (i == GapBegin)
                    {
                        if (GapEnd >= _buffer.Count) break;
                        i = GapEnd;
                    }
                    var (ch, style) = bufferSpan[i];
                    if (!style.Equals(currentStyle ??= style))
                    {
                        list.Add(new ValueTuple<string, Style>(sb.ToString(), currentStyle));
                        sb.Clear();
                        currentStyle = style;
                    }

                    sb.Append(ch);
                }

                if (sb.Length > 0) { list.Add(new ValueTuple<string, Style>(sb.ToString(), currentStyle!)); }

                return list;
            }
        }

        /// <inheritdoc cref="GapBuffer{TChar}(int)"/>
        public StylizedGapBuffer(int initialCapacity = DEFAULT_GAP_SIZE) : base(initialCapacity) { }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar}, int)"/>
        public StylizedGapBuffer(IEnumerable<ValueTuple<char, Style>> initialText, int initialCapacity) : base(initialText, initialCapacity) { }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar})"/>
        public StylizedGapBuffer(IEnumerable<ValueTuple<char, Style>> initialText) : base(initialText) { }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar})"/>
        public StylizedGapBuffer(ValueTuple<char, Style> initialText) : this(new[] { initialText }) { }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar})"/>
        public StylizedGapBuffer(in ValueTuple<string, Style> initialText) : this(initialText.Item1.AsStyledString(initialText.Item2)) { }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar}, int)"/>
        public StylizedGapBuffer(in ValueTuple<string, Style> initialText, int initialCapacity) : this(initialText.Item1, initialText.Item2, initialCapacity) { }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar}, int)"/>
        public StylizedGapBuffer(in ValueTuple<char, Style> initialText, int initialCapacity) : this(new string(initialText.Item1, 1), initialText.Item2, initialCapacity) { }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar}, int)"/>
        /// <param name="style">The <see cref="Style"/> of the text</param>
        public StylizedGapBuffer(string initialText, Style? style, int initialCapacity) : this(Math.Max(initialText.Length, initialCapacity)) { Insert(initialText, style); }

        /// <inheritdoc cref="GapBuffer{TChar}(IEnumerable{TChar})"/>
        public StylizedGapBuffer(string initialText, Style? style = null) : this(initialText.AsStyledString(style ?? Style.Plain), initialText.Length + DEFAULT_GAP_SIZE) { }

        /// <inheritdoc cref="GapBuffer{TChar}.Insert(IEnumerable{TChar})"/>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public StylizedGapBuffer Insert(string text, Style? style = null)
        {
            // Maybe come up with a better resizing function
            if (text.Length > GapSize) GrowGap(text.Length + _buffer.Count);

            var realStyle = style ?? Style.Plain;
            for (int i = 0; i < text.Length; i++) { _buffer[GapBegin + i] = new(text[i], realStyle); }

            GapBegin += text.Length;
            return this;
        }

        /// <inheritdoc cref="GapBuffer{TChar}.Insert(TChar)"/>
        public StylizedGapBuffer Insert(char text, Style? style = null) => Insert(text.ToString(), style);

        /// <inheritdoc cref="GapBuffer{TChar}.Erase"/>
        public new StylizedGapBuffer Erase(int count) => (StylizedGapBuffer)base.Erase(count);

        /// <inheritdoc cref="GapBuffer{TChar}.Backspace"/>
        public new StylizedGapBuffer Backspace(int count = 1) => (StylizedGapBuffer)base.Backspace(count);

        /// <inheritdoc cref="GapBuffer{TChar}.Delete"/>
        public new StylizedGapBuffer Delete(int count) => (StylizedGapBuffer)base.Delete(count);

        /// <inheritdoc cref="GapBuffer{TChar}.Move(int)"/>
        public new StylizedGapBuffer Move(int count) => (StylizedGapBuffer)base.Move(count);

        /// <inheritdoc cref="GapBuffer{TChar}.MoveAbs(int)"/>
        public new StylizedGapBuffer MoveAbs(int index) => (StylizedGapBuffer)base.MoveAbs(index);

        /// <inheritdoc cref="GapBuffer{TChar}.operator +(GapBuffer{TChar}, TChar)"/>
        public static StylizedGapBuffer operator +(StylizedGapBuffer b, in ValueTuple<char, Style> text) => (StylizedGapBuffer)b.Insert(text);

        /// <inheritdoc cref="GapBuffer{TChar}.operator +(GapBuffer{TChar}, TChar)"/>
        public static StylizedGapBuffer operator +(StylizedGapBuffer b, in ValueTuple<char, Style>[] text) => (StylizedGapBuffer)b.Insert(text);

        /// <inheritdoc cref="Text"/>
        /// <param name="b"><see cref="StylizedGapBuffer"/> instance</param>
        /// <returns><see cref="string"/> representation of gap buffer contents.</returns>
        public static explicit operator ValueTuple<char, Style>[](StylizedGapBuffer b) => b.Text;

        ///  <inheritdoc cref="GapBuffer{TChar}.operator +(GapBuffer{TChar}, IEnumerable{TChar})"/>
        public static StylizedGapBuffer operator +(StylizedGapBuffer b, IEnumerable<ValueTuple<char, Style>> text) => (StylizedGapBuffer)((GapBuffer<ValueTuple<char, Style>>)b + text);

        ///  <inheritdoc cref="GapBuffer{TChar}.operator >>(GapBuffer{TChar}, int)"/>
        public static StylizedGapBuffer operator >> (StylizedGapBuffer b, int count) => (StylizedGapBuffer)((GapBuffer<ValueTuple<char, Style>>)b >> count);

        ///  <inheritdoc cref="GapBuffer{TChar}.operator &lt;&lt;(GapBuffer{TChar}, int)"/>
        public static StylizedGapBuffer operator <<(StylizedGapBuffer b, int count) => (StylizedGapBuffer)((GapBuffer<ValueTuple<char, Style>>)b << count);
    }

    internal static class StringExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static ValueTuple<char, Style>[] ToStyledString(this string str, Style? style)
        {
            var buf = new ValueTuple<char, Style>[str.Length];
            var realStyle = style ?? Style.Plain;
            for (int i = 0; i < str.Length; i++) { buf[i] = new(str[i], realStyle); }
            return buf;
        }

        public static ValueTuple<char, Style>[] AsStyledString(this string str, Style? style) => ToStyledString(str, style);

        public static ValueTuple<char, Style> ToStyledCharacter(this char ch, Style? style) => new(ch, style ?? Style.Plain);

        public static ValueTuple<char, Style> AsStyledCharacter(this char ch, Style? style) => ToStyledCharacter(ch, style);
    }
}
