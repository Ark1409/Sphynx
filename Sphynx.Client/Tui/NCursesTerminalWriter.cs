// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using Mindmagma.Curses;
using Sphynx.Client.Utils;
using Sphynx.Utils;

namespace Sphynx.Client.Tui
{
    public sealed class NCursesTerminalWriter : IAsyncDisposable, IDisposable
    {
        private readonly Stream _stream;
        private readonly Encoding _encoding;

        private readonly NCurses.PutcFunc _putcFunc;

        // TODO: Refactor to "TermInfoDatabase" class
        public NCursesTerminalWriter(Stream stream) : this(stream, Encodings.UTF8) { }

        public NCursesTerminalWriter(Stream stream, Encoding encoding)
        {
            _stream = stream;
            _encoding = encoding;
            if (encoding.GetType().IsAssignableTo(typeof(UTF8Encoding)))
            {
                _putcFunc = c =>
                {
                    if (c != -1) Write((byte)c);
                    return c;
                };
            }
            else
            {
                var decoder = Encodings.UTF8.GetDecoder();
                var count = 0;
                _putcFunc = c =>
                {
                    if (c == -1) return c;
                    Span<char> chars = stackalloc char[1 + Encodings.UTF8.GetMaxCharCount(count)];
                    var decodeCount = decoder.GetChars([(byte)c], chars, false);
                    count++;
                    if (decodeCount > 0)
                    {
                        Write(chars[..decodeCount]);
                        decoder.Reset();
                        count = 0;
                    }
                    return c;
                };
            }
        }

        public void Write(ReadOnlySpan<char> s)
        {
            if (s.Length <= 0) return;

            const int POOL_THRESHOLD = 1024;
            var spLength = _encoding.GetMaxByteCount(s.Length);

            if (spLength >= POOL_THRESHOLD)
            {
                using var sp = ArrayPool<byte>.Shared.AutoRent(spLength);
                var count = _encoding.GetBytes(s, sp.AsSpan());
                Write(sp.AsSpan()[..count]);
            }
            else
            {
                Span<byte> sp = stackalloc byte[spLength];
                var count = _encoding.GetBytes(s, sp);
                Write(sp[..count]);
            }
        }

        public void Write(byte b)
        {
            _stream.WriteByte(b);
        }

        public void Write(Span<byte> b)
        {
            if (b.Length <= 0) return;
            _stream.Write(b);
        }

        public void WriteCodePoint(int codePoint)
        {
            Span<int> sp = [codePoint];
            WriteCodePoints(sp);
        }

        public void WriteCodePoints(ReadOnlySpan<int> codePoints)
        {
            var bytes = MemoryMarshal.AsBytes(codePoints);
            Write(Encodings.UTF32.GetString(bytes));
        }

        public void WriteCodePoint(Rune codePoint)
        {
            Span<char> s = stackalloc char[2];
            var count = codePoint.EncodeToUtf16(s);
            Write(s[..count]);
        }

        public void WriteCodePoints(ReadOnlySpan<Rune> codePoints)
        {
            if (codePoints.Length <= 0) return;
            const int POOL_THRESHOLD = 1024;
            var spLength = codePoints.Length * 2;
            if (spLength >= POOL_THRESHOLD)
            {
                using var sp = ArrayPool<char>.Shared.AutoRent(spLength);
                DoWrite(codePoints, sp);
            }
            else
            {
                Span<char> sp = stackalloc char[spLength];
                DoWrite(codePoints, sp);
            }

            void DoWrite(ReadOnlySpan<Rune> runes, Span<char> output)
            {
                var count = runes.FromUtf32String(output);
                Write(output[..count]);
            }
        }

        public void WriteGrapheme(in Grapheme g)
        {
            WriteCodePoints(g.Runes);
        }

        public void WriteGraphemes(ReadOnlySpan<Grapheme> graphemes)
        {
            for (int i = 0; i < graphemes.Length; i++)
            {
                WriteGrapheme(in graphemes[i]);
            }
        }

        public void WriteTermInfoString(string s)
        {
            if (s.Length <= 0) return;

            // Skip slow path if no encoded delay
            if (s.Contains("$<", StringComparison.Ordinal))
            {
                int affcnt = s.TerminalLineCount();
                // FIXME: tputs uses SCREEN* storage for getting terminfo data for delay/padding
                // _term.EnsureTerminalCurrent();
                NCurses.Tputs(s, affcnt, _putcFunc);
            }
            else
            {
                Write(s);
            }
        }

        public void Flush()
        {
            _stream.Flush();
        }

        public void Dispose()
        {
            _stream.Dispose();
            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            return _stream.DisposeAsync();
        }
    }
}
