using Spectre.Console.Rendering;
using Spectre.Console;
using Sphynx.Client.Utils;

namespace Sphynx.Client.UI
{
    internal class CharacterWrapParagraph : Renderable, IExpandable, IOverflowable
    {
        /// <inheritdoc/>
        public bool Expand { get; set; } = true;

        /// <inheritdoc/>
        public Overflow? Overflow { get; set; } = Spectre.Console.Overflow.Ellipsis;

        /// <summary>
        /// Gets or sets the width of the text box.
        /// </summary>
        public int? Width
        {
            get => !Expand ? _width : null;
            set
            {
                _width = value;
                Expand = _width is null;
            }
        }

        /// <summary>
        /// Gets or sets the height of the text box.
        /// </summary>
        public int? Height { get; set; }

        public int Lines => _lines.Count;

        private readonly LinkedList<LinkedList<ValueTuple<string, Style>>> _lines = new();
        private int? _lastWidth = null, _width = null;
        private int _xOffset = 0, _yOffset = 0;

        public CharacterWrapParagraph() { }

        public CharacterWrapParagraph(string text, Style? style = null)
        {
            Append(text, style);
        }

        /// <summary>
        /// Appends text to the current line.
        /// </summary>
        /// <param name="text">The text to add. New line characters will cause additional lines to be added.</param>
        /// <param name="style">The style for the text to use.</param>
        /// <returns>Same instance of <see cref="CharacterWrapParagraph"/></returns>
        public CharacterWrapParagraph Append(string text, Style? style = null) => Append(_lines.Count, text, style);

        /// <summary>
        /// Appends text to the desired line.
        /// </summary>
        /// <param name="lineNumber">A one-based index representing the to which the text should be added.</param>
        /// <param name="text">The text to add. New line characters will cause additional lines to be added.</param>
        /// <param name="style">The style for the text to use.</param>
        /// <returns>Same instance of <see cref="CharacterWrapParagraph"/></returns>
        public CharacterWrapParagraph Append(int lineNumber, string text, Style? style = null)
        {
            if (lineNumber <= 0)
            {
                for (int i = 0; i < -lineNumber + 1; i++)
                    _lines.AddFirst(new LinkedList<ValueTuple<string, Style>>());
                return Append(_lines.First!, text, style);
            }

            if (lineNumber <= _lines.Count)
            {
                int distFirst = lineNumber - 1;
                int distLast = _lines.Count - lineNumber;

                LinkedListNode<LinkedList<ValueTuple<string, Style>>> node;
                if (distFirst < distLast)
                {
                    node = _lines.First!;
                    for (int i = 0; i < distFirst; i++) node = node.Next!;
                }
                else
                {
                    node = _lines.Last!;
                    for (int i = 0; i < distLast; i++) node = node.Previous!;
                }
                return Append(node, text, style);
            }

            for (int i = 0; i < lineNumber - _lines.Count; i++)
                _lines.AddLast(new LinkedList<ValueTuple<string, Style>>());

            return Append(_lines.Last!, text, style);
        }

        /// <summary>
        /// Appends text to the desired line.
        /// </summary>
        /// <param name="line">A node which holds the line to which the text should appended.</param>
        /// <param name="text">The text to add. New line characters will cause additional lines to be added.</param>
        /// <param name="style">The style for the text to use.</param>
        /// <returns>Same instance of <see cref="CharacterWrapParagraph"/></returns>
        private CharacterWrapParagraph Append(LinkedListNode<LinkedList<ValueTuple<string, Style>>> line, string text, Style? style)
        {
            if (line.List != _lines) throw new ArgumentException("Line does not belong to this paragraph", nameof(line));

            text = text.RemoveTabs();
            string[] textLines = text.Split(new[] { Environment.NewLine, "\n" },
                StringSplitOptions.None);

            for (int i = 0; i < textLines.Length; i++)
            {
                string textLine = textLines[i];
                // User may be confused if when they manually add an empty string, it does not affect the text/lines contained within the text box
                // if (!string.IsNullOrEmpty(textLine))
                line.ValueRef.AddLast(new ValueTuple<string, Style>(textLine, style ?? Style.Plain));

                if (i < textLines.Length - 1) line = _lines.AddLast(new LinkedList<ValueTuple<string, Style>>());
            }

            return this;
        }

        public CharacterWrapParagraph AppendLine(string text, Style? style = null) => AppendLine(_lines.Count, text + Environment.NewLine, style);

        public CharacterWrapParagraph AppendLine(int lineNumber, string text, Style? style = null) => Append(lineNumber, text + Environment.NewLine, style);

        public CharacterWrapParagraph AppendLine(Style? style = null) => AppendLine(_lines.Count, style);

        public CharacterWrapParagraph AppendLine(int lineNumber, Style? style = null) => Append(lineNumber, Environment.NewLine, style);

        public CharacterWrapParagraph RemoveLine(int lineNumber)
        {
            if (lineNumber <= 0 || lineNumber > _lines.Count) throw new ArgumentOutOfRangeException(nameof(lineNumber));
            _lines.Remove(GetLineNode(lineNumber)!);
            return this;
        }

        public LinkedList<ValueTuple<string, Style>>? GetLine(int lineNumber) => GetLineNode(lineNumber)?.ValueRef;

        public LinkedListNode<LinkedList<ValueTuple<string, Style>>>? GetLineNode(int lineNumber) => lineNumber <= 0 ? null : _lines.GetNode(lineNumber - 1);

        /// <summary>
        /// Truncates the text box to the specified number of lines.
        /// If <c><paramref name="newLineCount"/> &lt; 0</c>, the truncation is performed in reverse (from bottom to top).
        /// </summary>
        /// <param name="newLineCount">The number of lines this text box should contain.</param>
        /// <returns></returns>
        public CharacterWrapParagraph TruncateLines(int newLineCount)
        {
            if (Math.Abs(newLineCount) > _lines.Count) return Append(newLineCount > 0 ? _lines.Count + 1 : 0, Environment.NewLine.Repeat(Math.Abs(newLineCount) - _lines.Count));

            int oldCount = _lines.Count;
            if (newLineCount >= 0)
            {
                for (int i = 0; i < oldCount - newLineCount; i++)
                {
                    _lines.RemoveLast();
                }
            }
            else
            {
                for (int i = 0; i < oldCount + newLineCount; i++)
                {
                    _lines.RemoveFirst();
                }
            }

            return this;
        }

        /// <summary>
        /// Scroll the text box <paramref name="count"/> row(s) up. 
        /// If <c><paramref name="count"/> &gt; 0</c>, this will shift all lines within the text box downwards.
        /// If <c><paramref name="count"/> &lt; 0</c>, this will shift all lines within the text box upwards.
        /// </summary>
        /// <param name="count">The number of rows by which the text box should shift down/up.</param>
        /// <returns><c>this</c></returns>
        public CharacterWrapParagraph ScrollUp(int count = 1)
        {
            _yOffset += -count;
            return this;
        }

        /// <summary>
        /// Scroll the text box <paramref name="count"/> row(s) down. 
        /// If <c><paramref name="count"/> &gt; 0</c>, this will shift all lines within the text box upwards.
        /// If <c><paramref name="count"/> &lt; 0</c>, this will shift all lines within the text box downwards.
        /// </summary>
        /// <param name="count">The number of rows by which the text box should shift up/down.</param>
        /// <returns><c>this</c></returns>
        public CharacterWrapParagraph ScrollDown(int count = 1) => ScrollUp(-count);

        /// <summary>
        /// Scroll the text box <paramref name="count"/> column(s) right. 
        /// If <c><paramref name="count"/> &gt; 0</c>, this will shift all lines within the text box to the left.
        /// If <c><paramref name="count"/> &lt; 0</c>, this will shift all lines within the text box to the right.
        /// </summary>
        /// <param name="count">The number of columns by which the text box should shift left/right.</param>
        /// <returns><c>this</c></returns>
        public CharacterWrapParagraph ScrollRight(int count = 1)
        {
            _xOffset += count;
            return this;
        }

        /// <summary>
        /// Scroll the text box <paramref name="count"/> column(s) left. 
        /// If <c><paramref name="count"/> &gt; 0</c>, this will shift all lines within the text box to the right.
        /// If <c><paramref name="count"/> &lt; 0</c>, this will shift all lines within the text box to the left.
        /// </summary>
        /// <param name="count">The number of columns by which the text box should shift right/left.</param>
        /// <returns><c>this</c></returns>
        public CharacterWrapParagraph ScrollLeft(int count = 1) => ScrollRight(-count);

        internal IEnumerable<Segment> DoRender(RenderOptions options, int maxWidth)
        {
            int trueWidth = Expand ? maxWidth : Math.Min(Width ?? maxWidth, maxWidth);
            // TODO _lines.Count as trueHeight might not work for folding
            int trueHeight = Height.HasValue ? Math.Min(Height.Value, options.Height ?? Height.Value) : options.Height ?? _lines.Count;

            _lastWidth = trueWidth;

            if (trueWidth <= 0 || trueHeight <= 0) return Enumerable.Empty<Segment>();

            var para = new Paragraph();

            int lineIndex = (Overflow ??= Spectre.Console.Overflow.Ellipsis) == Spectre.Console.Overflow.Fold ? 0 : Math.Max(0, _yOffset),
                lineCount = Math.Max(0, -_yOffset);
            int skipLines = Overflow == Spectre.Console.Overflow.Fold ? Math.Max(0, _yOffset) : 0;
            if (_yOffset < 0)
            {
                // There appears to be some sort of bug with Spectre.Console and new lines
                // Not only do we have to add an extra character before the new line character, 
                // for some reason we also cannot use Style.Plain (?)
                para.Append("\x0\n".Repeat(-_yOffset), Color.White);
            }

            for (var it = _lines.GetNode(lineIndex); it is not null && lineCount < trueHeight; skipLines--, lineCount++, lineIndex++, it = it.Next)
            {
                var line = it.ValueRef;
                switch (Overflow ?? Spectre.Console.Overflow.Ellipsis)
                {
                    case Spectre.Console.Overflow.Crop:
                        {
                            int totalLen = -_xOffset;
                            if (_xOffset < 0)
                            {
                                totalLen = Math.Min(totalLen, trueWidth);
                                para.Append(' '.Repeat(totalLen), Style.Plain);
                            }

                            foreach (var linePortion in line)
                            {
                                var (text, style) = linePortion;
                                if (-totalLen >= text.Length)
                                {
                                    totalLen += text.Length;
                                    continue;
                                }

                                int startIndex = Math.Max(0, -totalLen);

                                int useLen = Math.Min(text.Length - startIndex, trueWidth - Math.Max(0, totalLen));
                                para.Append(text.Substring(startIndex, useLen), style);
                                totalLen = Math.Max(0, totalLen);
                                if ((totalLen += useLen) >= trueWidth) break;
                            }
                            if (lineCount < trueHeight - 1)
                                para.Append("\x0\n", Color.White);
                        }
                        break;
                    case Spectre.Console.Overflow.Fold:
                        {
                            int currentLineLen = -_xOffset;

                            // If offset is negative (left), put correct padding on left side of text
                            if (_xOffset < 0)
                            {
                                currentLineLen = Math.Min(currentLineLen, trueWidth);
                                if (skipLines <= 0)
                                    para.Append(' '.Repeat(currentLineLen), Color.White);
                            }

                            for (var lineNode = line.First; lineNode is not null && lineCount < trueHeight; lineNode = lineNode.Next)
                            {
                                var (text, style) = lineNode.ValueRef;

                                if (-currentLineLen >= text.Length)
                                {
                                    currentLineLen += text.Length;
                                    continue;
                                }

                                for (int indexPos = Math.Max(0, -currentLineLen); indexPos < text.Length && lineCount < trueHeight;)
                                {
                                    int remainingWidth = trueWidth - Math.Max(0, currentLineLen);
                                    int useLen = Math.Min(text.Length - indexPos, remainingWidth);
                                    if (skipLines <= 0) { para.Append(text.Substring(indexPos, useLen), style); }
                                    currentLineLen = Math.Max(0, currentLineLen);
                                    if ((currentLineLen += useLen) >= trueWidth)
                                    {
                                        // Only append a new line if we're not already on the last printable line due to height restrictions
                                        if (lineCount < trueHeight - 1)
                                        {
                                            // Put padding after new line if there's still more text to print on the current @c line
                                            if (indexPos + useLen < text.Length || lineNode != line.Last)
                                            {
                                                const string linePadding = " ";
                                                if (skipLines <= 0)
                                                    para.Append($"\x0\n{linePadding}", Color.White);

                                                currentLineLen = Math.Max(0, -_xOffset);
                                                if (_xOffset < 0)
                                                {
                                                    currentLineLen = Math.Min(-_xOffset, trueWidth - linePadding.Length);
                                                    if (skipLines <= 1)
                                                        para.Append(' '.Repeat(currentLineLen), Color.White);
                                                }

                                                currentLineLen += linePadding.Length;
                                            }
                                            else
                                            {
                                                // Do nothing if we're on the last line of this loop
                                            }
                                        }
                                        // If we're at the end of the line, @c lineCount will be incremented by the outer for loop,
                                        // so there is no need to do it inside here
                                        // i.e. only increment if there's more to do on the current line
                                        if (indexPos + useLen < text.Length || lineNode != line.Last)
                                        {
                                            if (skipLines > 0)
                                                skipLines--;
                                            else lineCount++;
                                        }
                                    }

                                    indexPos += useLen;
                                }
                            }

                            // Append only if not on very last line
                            if (lineCount < trueHeight - 1)
                            {
                                // An extra space is needed to force a new line when there's no characters on
                                // the current line because Spectre.Console is buggy
                                // para.Append(currentLineLen > 0 ? "\n" : "\x0\n", Color.White);
                                if (skipLines <= 0)
                                    para.Append("\x0\n", Color.White);
                                else lineCount--;
                            }
                        }
                        break;
                    case Spectre.Console.Overflow.Ellipsis:
                        {
                            const string unicodeEllipsis = "\u2026", asciiEllipsis = "...";
                            var ellipsis = options.Unicode ? unicodeEllipsis : asciiEllipsis;
                            int totalLen = -_xOffset; // Total amount of length currently used. Negative for _xOffset>0
                            switch (_xOffset)
                            {
                                case < 0:
                                    totalLen = Math.Min(totalLen, trueWidth - ellipsis.Length);
                                    para.Append(' '.Repeat(totalLen), Style.Plain);
                                    break;
                                case > 0:
                                    // TODO Make ellipsis color match the color of the text actually being partially hidden
                                    int ellipsisLen = Math.Min(ellipsis.Length, trueWidth);
                                    para.Append(ellipsis[..ellipsisLen], line.First?.ValueRef.Item2 ?? Style.Plain);
                                    // totalLen -= ellipsisLen - 1;
                                    break;
                            }

                            foreach (var linePortion in line)
                            {
                                var (text, style) = linePortion;
                                if (-totalLen >= text.Length)
                                {
                                    totalLen += text.Length;
                                    continue;
                                }

                                int remainingWidth = trueWidth - Math.Max(0, totalLen) + (_xOffset > 0 ? -ellipsis.Length : 0);

                                if (remainingWidth <= 0) break;

                                int startIndex = Math.Max(0, -totalLen);
                                int textLen = text.Length - startIndex;

                                int useLen = textLen > remainingWidth ? Math.Max(0, remainingWidth - ellipsis.Length) : textLen;
                                para.Append(text.Substring(startIndex, useLen), style);
                                totalLen = Math.Max(0, totalLen) + useLen;
                                if (textLen > remainingWidth)
                                {
                                    para.Append(ellipsis, style);
                                    totalLen += ellipsis.Length;
                                    break;
                                }
                            }
                            if (lineCount < trueHeight - 1)
                                para.Append("\x0\n", Color.White);
                        }
                        break;
                    default: break;
                }
            }

            if (-_yOffset + _lines.Count < trueHeight)
            {
                para.Append("\x0\n".Repeat(trueHeight - (-_yOffset + _lines.Count)), Color.White);
            }

            return para.Crop().LeftJustified().GetSegments(AnsiConsole.Console);
        }

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => DoRender(options, maxWidth);
    }
}
