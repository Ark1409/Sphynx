using Spectre.Console;
using Spectre.Console.Rendering;
using Sphynx.Client.Utils;

namespace Sphynx.Client.UI
{
    public class TextBox : Renderable, IExpandable, IOverflowable, IFocusable
    {
        /// <inheritdoc/>
        public bool Expand { get; set; } = true;

        /// <inheritdoc/>
        public Overflow? Overflow { get; set; } = Spectre.Console.Overflow.Ellipsis;

        /// <summary>
        /// Gets or sets the width of the text box.
        /// Setting this to a value other than <c>null</c> implicitly disables <see cref="Expand"/>
        /// </summary>
        public int? Width
        {
            get => Expand ? null : _width;
            set
            {
                _width = value ?? 0;
                Expand = value is null;
            }
        }

        /// <summary>
        /// Gets or sets the height of the text box.
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// Gets the number of lines in the text box
        /// </summary>
        public int Lines
        {
            get
            {
                int lineCount = 1;
                var buf = _buffer.Buffer;
                for (int i = 0; i < _buffer.GapBegin; i++)
                {
                    if (buf[i].Item1 == '\n') lineCount++;
                }
                for (int i = _buffer.GapEnd; i < _buffer.BufferCount; i++)
                {
                    if (buf[i].Item1 == '\n') lineCount++;
                }
                return lineCount;
            }
        }

        /// <summary>
        /// Gets or sets the index of the cursor in the text box
        /// </summary>
        public int Cursor
        {
            get => _buffer.GapBegin;
            set => _buffer.MoveAbs(value);
        }

        /// <summary>
        /// Gets the characters at the specified position.
        /// This value is copied and therefore does not effect the text box itself.
        /// </summary>
        /// <param name="pos">The position of the character</param>
        /// <exception cref="ArgumentOutOfRangeException">If <c>pos &ge; Length</c> or <c>pos &lt; 0</c></exception>
        public ValueTuple<char, Style> this[int pos] =>
            pos >= _buffer.Length || pos < 0
                ? throw new ArgumentOutOfRangeException(nameof(pos))
                : (pos < _buffer.GapBegin ? _buffer.Buffer[pos] : _buffer.Buffer[_buffer.GapEnd + 1 + pos - _buffer.GapBegin]);

        /// <summary>
        /// Obtains the <see cref="StylizedGapBuffer"/> used for storing the styled text.
        /// </summary>
        internal StylizedGapBuffer Buffer => _buffer;

        /// <summary>
        /// Gets the on-screen cursor position as a (row, column) pair, relative to the top-left of the text box,
        /// based on the width and height of this object as it was last rendered.
        /// The returned point holds the one-based (row, column) numbers of the cursor.
        /// Returns <see cref="Point2i.Empty"/> if the cursor is outside the current text box view and should not be shown.
        /// </summary>
        public Point2i LastCursorPosition => CalculateCursorPosition(LastWidth, LastHeight);

        /// <summary>
        /// Gets the width of the text box as it was last rendered.
        /// Guaranteed to be <c>0</c> if the text box has not yet been rendered.
        /// </summary>
        public int LastWidth { get; private set; } = 0;

        /// <summary>
        /// Gets the width of the text box as it was last rendered.
        /// Guaranteed to be <c>0</c> if the text box has not yet been rendered.
        /// </summary>
        public int LastHeight { get; private set; } = 0;

        /// <summary>
        /// Gets or sets the (horizontal) auto scrolling property for the text box.
        /// </summary>
        public bool AutoScrollX { get; set; } = true;

        /// <summary>
        /// Gets or sets the (vertical) auto scrolling property for the text box.
        /// </summary>
        public bool AutoScrollY { get; set; } = true;

        /// <summary>
        /// Gets or sets the x-offset at which text should be displayed.
        /// Settings this to a value other than <c>null</c> implicitly disables <see cref="AutoScrollX"/>.
        /// </summary>
        public int? XOffset
        {
            get => AutoScrollX ? null : _xPos;
            set
            {
                if (value is null) { AutoScrollX = true; }
                else
                {
                    _xPos = value.Value;
                    AutoScrollX = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the y-offset at which text should be displayed.
        /// Settings this to a value other than <c>null</c> implicitly disables <see cref="AutoScrollY"/>.
        /// </summary>
        public int? YOffset
        {
            get => AutoScrollY ? null : _yPos;
            set
            {
                if (value is null) { AutoScrollY = true; }
                else
                {
                    _yPos = value.Value;
                    AutoScrollY = false;
                }
            }
        }

        private readonly StylizedGapBuffer _buffer;
        private int _width = 0;
        private int _xPos = 0, _yPos = 0;

        public TextBox()
        {
            _buffer = new StylizedGapBuffer();
        }

        public TextBox(in ValueTuple<string, Style> text) : this(text.Item1, text.Item2) { }

        public TextBox(in ValueTuple<char, Style> text) : this(new string(text.Item1, 1), text.Item2) { }

        public TextBox(string text, Style? style = null)
        {
            _buffer = new StylizedGapBuffer(text, style ?? Style.Plain);
        }

        public TextBox(IEnumerable<ValueTuple<string, Style>> texts)
        {
            _buffer = new StylizedGapBuffer(StylizedGapBuffer.DEFAULT_GAP_SIZE * 2);
            foreach (var (text, style) in texts)
            {
                _buffer.Insert(text, style);
            }
        }

        public TextBox Insert(string str, Style? style = null)
        {
            if (style is null)
            {
                if (_buffer.Length > 0)
                {
                    style = _buffer.GapBegin > 0 ? _buffer.Buffer[_buffer.GapBegin - 1].Item2 : _buffer.Buffer[_buffer.GapEnd].Item2;
                }
                else
                {
                    style = Style.Plain;
                }
            }

            _buffer.Insert(str, style);

            return this;
        }

        public TextBox Insert(in ValueTuple<string, Style> text) => Insert(text.Item1, text.Item2);

        public TextBox Insert(in ValueTuple<char, Style> text) => Insert(new string(text.Item1, 1), text.Item2);

        public TextBox Insert(char str, Style? style = null) => Insert(new string(str, 1), style);

        public TextBox InsertLine() => Insert('\n');

        public TextBox InsertLine(string str, Style? style = null) => Insert(str + '\n', style);

        public TextBox InsertLine(in ValueTuple<string, Style> text) => Insert(text.Item1 + '\n', text.Item2);

        public TextBox InsertLine(in ValueTuple<char, Style> text) => Insert(new string(new[] { text.Item1, '\n' }), text.Item2);

        public TextBox InsertLine(char str, Style? style = null) => Insert(new string(new[] { str, '\n' }), style);

        public TextBox Append(string str, Style? style = null)
        {
            int oldPos = Cursor;
            _buffer.MoveAbs(_buffer.BufferCount - _buffer.GapSize);
            InsertLine(str, style);
            Cursor = oldPos;
            return this;
        }

        public TextBox Append(in ValueTuple<string, Style> text)
        {
            int oldPos = Cursor;
            _buffer.MoveAbs(_buffer.BufferCount - _buffer.GapSize);
            Insert(text);
            Cursor = oldPos;
            return this;
        }

        public TextBox Append(in ValueTuple<char, Style> text)
        {
            int oldPos = Cursor;
            _buffer.MoveAbs(_buffer.BufferCount - _buffer.GapSize);
            Insert(text);
            Cursor = oldPos;
            return this;
        }

        public TextBox Append(char str, Style? style = null)
        {
            int oldPos = Cursor;
            _buffer.MoveAbs(_buffer.BufferCount - _buffer.GapSize);
            Insert(str, style);
            Cursor = oldPos;
            return this;
        }

        public TextBox AppendLine() => Append('\n');

        public TextBox AppendLine(string str, Style? style = null) => Append(str + '\n', style);

        public TextBox AppendLine(in ValueTuple<string, Style> text) => Append(text.Item1 + '\n', text.Item2);

        public TextBox AppendLine(in ValueTuple<char, Style> text) => Append(new string(new[] { text.Item1, '\n' }), text.Item2);

        public TextBox AppendLine(char str, Style? style = null) => Append(new string(new[] { str, '\n' }), style);

        public TextBox Erase(int count)
        {
            _buffer.Erase(count);
            return this;
        }

        public TextBox EraseWord(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                var buffer = _buffer.Buffer;
                int cursorIndex = Math.Max(0, Cursor - 1);
                for (; cursorIndex > 0 && buffer[cursorIndex].Item1 == ' '; cursorIndex--) { }
                for (; cursorIndex > 0 && buffer[cursorIndex].Item1 != ' '; cursorIndex--) { }
                _buffer.Erase(Cursor - cursorIndex);     
            }
            return this;
        }

        public TextBox DeleteWord(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                var buffer = _buffer.Buffer;
                int wordEnd = Math.Min(_buffer.BufferCount, _buffer.GapEnd);
                for (; wordEnd < _buffer.BufferCount && buffer[wordEnd].Item1 == ' '; wordEnd++) { }
                for (; wordEnd < _buffer.BufferCount && buffer[wordEnd].Item1 != ' '; wordEnd++) { }
                _buffer.Delete(wordEnd - _buffer.GapEnd);
            }
            return this;
        }

        public TextBox Backspace(int count = 1) => Erase(count);

        public TextBox Delete(int count = 1)
        {
            _buffer.Delete(count);
            return this;
        }

        public TextBox MoveWord(int count)
        {
            if (count >= 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var buffer = _buffer.Buffer;
                    int wordEnd = Math.Min(_buffer.BufferCount, _buffer.GapEnd);
                    for (; wordEnd < _buffer.BufferCount && buffer[wordEnd].Item1 == ' '; wordEnd++) { }
                    for (; wordEnd < _buffer.BufferCount && buffer[wordEnd].Item1 != ' '; wordEnd++) { }
                    _buffer.MoveAbs(wordEnd - _buffer.GapSize);
                }
            }
            else
            {
                for (int i = 0; i < -count; i++)
                {
                    var buffer = _buffer.Buffer;
                    int cursorIndex = Math.Max(0, Cursor - 1);
                    for (; cursorIndex > 0 && buffer[cursorIndex].Item1 == ' '; cursorIndex--) { }
                    for (; cursorIndex > 0 && buffer[cursorIndex].Item1 != ' '; cursorIndex--) { }
                    _buffer.MoveAbs(cursorIndex);
                }
            }
            return this;
        }

        /// <summary>
        /// Moves the <see cref="Cursor"/> to the previous/next line while maintaining the same horizontal position (column number).
        /// </summary>
        /// <param name="count">The amount of lines to move. The default is <c>1</c>. Can be negative for movement in the opposite direction.</param>
        /// <returns><c>this</c></returns>
        public TextBox NextLine(int count = 1)
        {
            if (count == 0) { return this; }
            int currentLineStart = 0;
            var buf = _buffer.Buffer;

            for (int i = Cursor - 1; i >= 0; i--)
            {
                if (buf[i].Item1 == '\n')
                {
                    currentLineStart = i + 1;
                    break;
                }
            }
            int cursorColPos = Cursor - currentLineStart;

            int lineStart = currentLineStart;

            if (count > 0)
            {
                for (int i = _buffer.GapEnd, lineCount = 0; i < _buffer.BufferCount && lineCount < count; i++)
                {
                    if (buf[i].Item1 == '\n')
                    {
                        lineStart = i + 1 - _buffer.GapSize;
                        lineCount++;
                    }
                }
            }
            else
            {
                int lineCount = 0;
                for (int i = currentLineStart - 2; i >= 0 && lineCount < -count; i--)
                {
                    if (buf[i].Item1 == '\n')
                    {
                        lineStart = i + 1;
                        lineCount++;
                    }
                }
                if (lineCount < -count) lineStart = 0;
            }

            int lineLength = 0;
            for (int i = lineStart >= _buffer.GapBegin ? lineStart + _buffer.GapSize : lineStart;
                 i < _buffer.BufferCount && lineLength < cursorColPos;
                 i++, lineLength++)
            {
                if (buf[i].Item1 == '\n') break;
            }
            Cursor = lineStart + Math.Min(cursorColPos, lineLength);

            return this;
        }

        /// <inheritdoc cref="NextLine"/>
        public TextBox PrevLine(int count = 1) => NextLine(-count);

        /// <summary>
        /// Truncates the text box to the specified number of lines.
        /// If <c>newLineCount &lt; 0</c>, the truncation is performed in reverse (from bottom to top).
        /// </summary>
        /// <param name="newLineCount">The number of lines this text box should contain.</param>
        public void TruncateLines(int newLineCount)
        {
            var linePositions = new List<int>();
            {
                var buf = _buffer.Buffer;
                for (int i = 0; i < _buffer.GapBegin; i++)
                {
                    if (buf[i].Item1 == '\n')
                    {
                        linePositions.Add(i + 1);
                    }
                }

                for (int i = _buffer.GapEnd; i < _buffer.BufferCount; i++)
                {
                    if (buf[i].Item1 == '\n')
                    {
                        linePositions.Add(i + 1 - _buffer.GapBegin);
                    }
                }
            }
            if (Math.Abs(newLineCount) > linePositions.Count)
                throw new ArgumentOutOfRangeException(nameof(newLineCount), "Cannot truncate to greater line size");

            if (newLineCount >= 0)
            {
                int oldPos = Cursor;

                Cursor = linePositions[newLineCount - 1] - 1;
                _buffer.Delete(_buffer.Length - Cursor);

                Cursor = Math.Min(oldPos, _buffer.Length - 1);
            }
            else
            {
                int oldPos = Cursor;

                Cursor = linePositions[linePositions.Count + newLineCount];
                _buffer.Backspace(Cursor);

                Cursor = Math.Max(0, oldPos - Cursor);
            }
        }

        internal IEnumerable<Segment> DoRender(RenderOptions options, int maxWidth)
        {
            var para = new CharacterWrapParagraph();

            foreach (var (str, style) in _buffer.TextBlocks)
            {
                para.Append(str, style);
            }

            para.Expand = Expand;
            para.Width = Width;
            para.Height = Height;
            para.Overflow = Overflow ??= Spectre.Console.Overflow.Ellipsis;

            int trueWidth = Expand ? maxWidth : Math.Min(Width ?? maxWidth, maxWidth);

            int lineCount = Lines;
            // TODO para.Lines as trueHeight might not work for folding
            int trueHeight = Height.HasValue ? Math.Min(Height.Value, options.Height ?? Height.Value) : options.Height ?? lineCount;

            LastWidth = trueWidth;
            LastHeight = trueHeight;
            if (AutoScrollY && Overflow == Spectre.Console.Overflow.Fold)
            {
                var buf = _buffer.Buffer;

                int lineNumber = 1;
                int lineCol = 1 + -_xPos;
                int lastLineBegin = _xPos;
                bool lastFolded = false;
                for (int i = 0; i < Cursor; i++, lineCol++)
                {
                    bool willFold = i - lastLineBegin + (lastFolded ? 2 : 0) >= trueWidth;
                    if (willFold || buf[i].Item1 == '\n')
                    {
                        lineNumber++;
                        lastLineBegin = i + 1 + (willFold ? -Math.Max(0, -_xPos) : _xPos);
                        lineCol = willFold ? 2 + Math.Max(0, -_xPos) : -_xPos;
                        lastFolded = willFold;
                    }
                }

                {
                    int diff = (lineNumber - 1) - _yPos;
                    if (diff < 0)
                    {
                        _yPos += diff;
                    }
                    else if (diff >= trueHeight)
                    {
                        _yPos += diff - trueHeight + 1;
                    }
                    _yPos = Math.Clamp(_yPos, 0, Math.Max(0, lineNumber - trueHeight));
                }
            }
            else if ((AutoScrollX || AutoScrollY) && Overflow != Spectre.Console.Overflow.Fold)
            {
                var buf = _buffer.Buffer;
                int lineNumber = 1;
                int lastLineBegin = 0;
                for (int i = 0; i < Cursor /* == _buffer.GapBegin */; i++)
                {
                    if (buf[i].Item1 == '\n')
                    {
                        lineNumber++;
                        lastLineBegin = i + 1;
                    }
                }
                int linePos = Cursor - lastLineBegin;

                if (AutoScrollX)
                {
                    int firstPageDiv = Math.Sign(linePos / (trueWidth + 1));
                    int otherPageDiv;
                    switch (Overflow)
                    {
                        case Spectre.Console.Overflow.Ellipsis:
                            otherPageDiv = Math.Max(0, (linePos - trueWidth - 1) / (trueWidth - 1));
                            _xPos = Math.Max(0, firstPageDiv * trueWidth + otherPageDiv * (trueWidth - 1));
                            break;
                        default:
                            otherPageDiv = Math.Max(0, (linePos - trueWidth - 1) / (trueWidth));
                            _xPos = Math.Max(0, (firstPageDiv + otherPageDiv) * trueWidth);
                            break;
                    }
                }

                if (AutoScrollY)
                {
                    int diff = (lineNumber - 1) - _yPos;
                    if (diff < 0)
                    {
                        _yPos += diff;
                    }
                    else if (diff >= trueHeight)
                    {
                        _yPos += diff - trueHeight + 1;
                    }
                    _yPos = Math.Clamp(_yPos, 0, Math.Max(0, lineCount - trueHeight));
                }
            }
            para.ScrollRight(_xPos);
            para.ScrollDown(_yPos);

            return para.DoRender(options, maxWidth);
        }

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth) => DoRender(options, maxWidth);

        /// <summary>
        /// Calculates the (relative) cursor position based on the current settings and the supplied <paramref name="width"/> and <paramref name="height"/>.
        /// The returned point holds the one-based (row, column) numbers of the cursor.
        /// </summary>
        /// <param name="width">The width to use in calculation.</param>
        /// <param name="height">The height to use in calculation.</param>
        /// <returns>A point representing the cursor position relative to the top-left of the text box.
        /// <see cref="Point2i.Empty"/> if the cursor is outside the current text box view and should not be shown.</returns>
        public Point2i CalculateCursorPosition(int width, int height)
        {
            if (width <= 0 || height <= 0) return Point2i.Empty;

            int lineCount = Lines;

            int xPos = _xPos;
            int yPos = _yPos;

            switch (Overflow ??= Spectre.Console.Overflow.Ellipsis)
            {
                case Spectre.Console.Overflow.Crop:
                case Spectre.Console.Overflow.Ellipsis:
                    {
                        var buf = _buffer.Buffer;

                        int lineNumber = 1;
                        int lastLineBegin = 0;
                        for (int i = 0; i < Cursor /* == _buffer.GapBegin */; i++)
                        {
                            if (buf[i].Item1 == '\n')
                            {
                                lineNumber++;
                                lastLineBegin = i + 1;
                            }
                        }
                        int linePos = Cursor - lastLineBegin;

                        if (AutoScrollX)
                        {
                            // Cursor has to start one character ahead when switching to 2nd page and onwards
                            int firstPageDiv = Math.Sign(linePos / (width + 1));
                            int otherPageDiv;
                            switch (Overflow)
                            {
                                case Spectre.Console.Overflow.Ellipsis:
                                    linePos += Math.Sign(firstPageDiv);
                                    otherPageDiv = Math.Max(0, (linePos - 1 - width - 1) / (width - 1));
                                    xPos = Math.Max(0, (firstPageDiv * width + otherPageDiv * (width - 1)));
                                    break;
                                default:
                                    otherPageDiv = Math.Max(0, (linePos - width - 1) / width);
                                    xPos = Math.Max(0, (firstPageDiv + otherPageDiv) * width);
                                    break;
                            }
                        }

                        if (AutoScrollY)
                        {
                            int diff = (lineNumber - 1) - yPos;
                            if (diff < 0)
                            {
                                yPos += diff;
                            }
                            else if (diff >= height)
                            {
                                yPos += diff - height + 1;
                            }
                            yPos = Math.Clamp(yPos, 0, Math.Max(0, lineCount - height));
                        }

                        linePos -= xPos;
                        lineNumber -= yPos;

                        return new Point2i(linePos > width ? 0 : linePos + 1, lineNumber > height || lineNumber < 0 ? 0 : lineNumber);
                    }
                case Spectre.Console.Overflow.Fold:
                    {
                        var buf = _buffer.Buffer;

                        int lineNumber = 1 + -yPos;
                        int lineCol = 1 + -xPos;
                        int lastLineBegin = xPos;
                        bool lastFolded = false;
                        for (int i = 0; i < Cursor; i++, lineCol++)
                        {
                            bool willFold = i - lastLineBegin + (lastFolded ? 2 : 0) >= width;
                            if (willFold || buf[i].Item1 == '\n')
                            {
                                lineNumber++;
                                lastLineBegin = i + 1 + (willFold ? -Math.Max(0, -xPos) : xPos);
                                lineCol = willFold ? 2 + Math.Max(0, -xPos) : -xPos;
                                lastFolded = willFold;
                            }
                        }
                        return new Point2i(lineCol - 1 > width || lineCol < 0 ? 0 : lineCol, lineNumber > height || lineNumber < 0 ? 0 : lineNumber);
                    }
                default:
                    return Point2i.Empty;
            }
        }

        /// <summary>
        /// Calculates the (relative) cursor position based on the current settings and the supplied <paramref name="width"/>.
        /// <see cref="Height"/> is use in the calculation if defined, otherwise <see cref="Lines"/> is used as the text box's height.
        /// The returned point holds the one-based (row, column) numbers of the cursor.
        /// </summary>
        /// <param name="width">The width to use in calculation.</param>
        /// <returns>A point representing the cursor position relative to the top-left of the text box.
        /// <see cref="Point2i.Empty"/> if the cursor is outside the current text box view and should not be shown.</returns>
        public Point2i CalculateCursorPosition(int width) => CalculateCursorPosition(width, Height ?? Lines);

        /// <summary>
        /// Calculates the (relative) cursor position based on the current settings and the current <see cref="Width"/>.
        /// The returned point holds the one-based (row, column) numbers of the cursor.
        /// </summary>
        /// <returns>A point representing the cursor position relative to the top-left of the text box.
        /// <see cref="Point2i.Empty"/> if the cursor is outside the current text box view and should not be shown.</returns>
        /// <exception cref="ArgumentNullException">If <see cref="Width"/> is <c>null</c>.</exception>
        public Point2i CalculateCursorPosition() => CalculateCursorPosition(Width ?? throw new ArgumentNullException(nameof(Width)));

        /// <summary>
        /// Scroll the text box <paramref name="count"/> row(s) up. 
        /// If <c><paramref name="count"/> &gt; 0</c>, this will shift all lines within the text box downwards.
        /// If <c><paramref name="count"/> &lt; 0</c>, this will shift all lines within the text box upwards.
        /// </summary>
        /// <param name="count">The number of rows by which the text box should shift down/up.</param>
        /// <returns><c>this</c>.</returns>
        public TextBox ScrollUp(int count = 1)
        {
            _yPos = Math.Max(0, _yPos - count);
            return this;
        }

        /// <summary>
        /// Scroll the text box <paramref name="count"/> row(s) down. 
        /// If <c><paramref name="count"/> &gt; 0</c>, this will shift all lines within the text box upwards.
        /// If <c><paramref name="count"/> &lt; 0</c>, this will shift all lines within the text box downwards.
        /// </summary>
        /// <param name="count">The number of rows by which the text box should shift up/down.</param>
        /// <returns><c>this</c>.</returns>
        public TextBox ScrollDown(int count = 1) => ScrollUp(-count);

        /// <summary>
        /// Scroll the text box <paramref name="count"/> column(s) right. 
        /// If <c><paramref name="count"/> &gt; 0</c>, this will shift all lines within the text box to the left.
        /// If <c><paramref name="count"/> &lt; 0</c>, this will shift all lines within the text box to the right.
        /// </summary>
        /// <param name="count">The number of columns by which the text box should shift left/right.</param>
        /// <returns><c>this</c>.</returns>
        public TextBox ScrollRight(int count = 1)
        {
            _xPos = Math.Max(0, _xPos + count);
            return this;
        }

        /// <summary>
        /// Scroll the text box <paramref name="count"/> column(s) left. 
        /// If <c><paramref name="count"/> &gt; 0</c>, this will shift all lines within the text box to the right.
        /// If <c><paramref name="count"/> &lt; 0</c>, this will shift all lines within the text box to the left.
        /// </summary>
        /// <param name="count">The number of columns by which the text box should shift right/left.</param>
        /// <returns><c>this</c>.</returns>
        public TextBox ScrollLeft(int count = 1) => ScrollRight(-count);

        public bool HandleKey(in ConsoleKeyInfo key)
        {
            char keyChar = key.KeyChar;
            switch (keyChar)
            {
                case '\r':
                    InsertLine();
                    return true;
                case '\t':
                    Insert("    ");
                    return true;
                default:
                    if (keyChar.IsLatin1Printable())
                    {
                        Insert(keyChar);
                        return true;
                    }
                    break;
            }

            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                        MoveWord(-1);
                    else
                        Cursor--;
                    return true;
                case ConsoleKey.RightArrow:
                    if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                        MoveWord(1);
                    else
                        Cursor++;
                    return true;
                case ConsoleKey.UpArrow:
                    PrevLine();
                    return true;
                case ConsoleKey.DownArrow:
                    NextLine();
                    return true;
                case ConsoleKey.Backspace:
                    if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                        EraseWord();
                    else
                        Backspace();
                    return true;
                case ConsoleKey.Delete:
                    if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
                        DeleteWord();
                    else
                        Delete();
                    return true;
                case ConsoleKey.PageUp:
                    PrevLine((Height ?? LastHeight) * 3 / 2);
                    return true;
                case ConsoleKey.PageDown:
                    NextLine((Height ?? LastHeight) * 3 / 2);
                    return true;
            }

            return false;
        }

        public static TextBox operator +(TextBox textBox, in ValueTuple<char, Style> text) => textBox.Insert(text);
        public static TextBox operator +(TextBox textBox, in ValueTuple<string, Style> text) => textBox.Insert(text);
        public static TextBox operator +(TextBox textBox, string text) => textBox.Insert(text);
        public static TextBox operator +(TextBox textBox, char text) => textBox.Insert(text);

        public static TextBox operator >> (TextBox textBox, int count)
        {
            textBox.Cursor += count;
            return textBox;
        }

        public static TextBox operator <<(TextBox textBox, int count) => textBox >> (-count);

        public static TextBox operator ^(TextBox textBox, int count) => textBox.PrevLine(count);
    }
}
