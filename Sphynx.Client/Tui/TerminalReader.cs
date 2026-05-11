// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Sphynx.Client.Utils;
using Sphynx.Collections;
using Sphynx.Streams;
using Sphynx.Utils;

namespace Sphynx.Client.Tui
{
    public class TerminalReader : IDisposable, IAsyncDisposable
    {
        private readonly Encoding _encoding;
        private readonly TimeoutStream _timeoutStream;
        private readonly RewindableStream _stream;

        private int _disposed = 0;
        public bool ThrowOnTimeout
        {
            get => _timeoutStream.ThrowOnTimeout;
            set => _timeoutStream.ThrowOnTimeout = value;
        }

        public bool IsEof => _stream.MinimumPending <= 0 && _timeoutStream.IsEof;

        public TerminalReader(Stream input, Encoding encoding)
        {
            _encoding = encoding;
            _timeoutStream = new TimeoutStream(input, 1024) { ThrowOnTimeout = false };
            _stream = new RewindableStream(_timeoutStream);
        }

        /// <summary>
        /// Retrieves a lower bound on the number of bytes that can be Read without blocking.
        /// </summary>
        public int MinimumPending => _stream.MinimumPending;
        private void SetTimeout(TimeSpan timeout)
        {
            _stream.ReadTimeout = timeout == Timeout.InfiniteTimeSpan ? -1 : (int)Math.Ceiling(timeout.TotalMilliseconds);
        }

        public int ReadByte()
        {
            return ReadByte(Timeout.InfiniteTimeSpan);
        }

        public int ReadByte(TimeSpan timeout)
        {
            ThrowIfDisposed();
            SetTimeout(timeout);
            return _stream.ReadByte();
        }

        public int ReadBytes(Span<byte> sp, bool tryFill = true)
        {
            return ReadBytes(sp, Timeout.InfiniteTimeSpan, tryFill);
        }

        private readonly Stopwatch _bytesWatch = new();
        private static readonly Stream _out = Console.OpenStandardError();
        private TimeSpan _readByteTimeoutSum = TimeSpan.Zero;
        public int ReadBytes(Span<byte> sp, TimeSpan timeout, bool tryFill = true)
        {
            ThrowIfDisposed();
            if (sp.Length <= 0) return 0;

            var readCount = 0;
            var minimum = MinimumPending;
            while (readCount < sp.Length)
            {
                SetTimeout(timeout);
                var newReadCount = 0;
                _bytesWatch.Restart();
                try
                {
                    newReadCount = _stream.Read(sp[readCount..]);
                }
                catch (TimeoutException) { }
                _bytesWatch.Stop();

                readCount += newReadCount;

                var elapsed = _bytesWatch.Elapsed;
                _readByteTimeoutSum += elapsed;

                if (!tryFill) break;
                if (newReadCount <= 0) break;
                if (readCount <= minimum)
                {
                    _readByteTimeoutSum -= elapsed;
                    continue;
                }
                _out.Write(sp.Slice(readCount - newReadCount, newReadCount));
                if (timeout != Timeout.InfiniteTimeSpan)
                    timeout = elapsed > timeout ? TimeSpan.Zero : timeout - elapsed;
            }
            if (readCount <= 0 && ThrowOnTimeout && !IsEof) throw new TimeoutException();
            return readCount;
        }

        public int ReadCodePoint()
        {
            return ReadCodePoint(Timeout.InfiniteTimeSpan);
        }

        public int ReadCodePoint(TimeSpan timeout)
        {
            ThrowIfDisposed();

            Span<byte> bytes = stackalloc byte[_encoding.GetMaxByteCount(2)];
            Span<char> chars = stackalloc char[2];

            int cp = -1;

            int i = 0;
            var totalReadCount = 0;

            var decoder = _encoding.GetDecoder();
            for (; i < bytes.Length; i++)
            {
                int b = -1;
                if (i >= totalReadCount) DoReadByte(bytes, ref timeout, ref totalReadCount);
                if (i >= totalReadCount) break;
                b = bytes[i];

                var charsWritten = decoder.GetChars(bytes[i..(i + 1)], chars, false);
                if (charsWritten <= 0) continue;

                if (charsWritten == 1)
                {
                    if (char.IsSurrogate(chars[0])) continue;
                    cp = chars[0];
                    break;
                }
                Debug.Assert(charsWritten == 2);
                if (char.IsHighSurrogate(chars[1]))
                {
                    (chars[0], chars[1]) = (chars[1], chars[0]);
                }
                if (!char.IsSurrogatePair(chars[0], chars[1]))
                {
                    decoder.Reset();
                    i = -1;
                    totalReadCount = 0;
                    continue;
                }
                cp = char.ConvertToUtf32(chars[0], chars[1]);
                break;
            }

            bytes = bytes[..totalReadCount];
            if (timeout != Timeout.InfiniteTimeSpan)
            {
                if (cp == -1)
                {
                    if (!IsEof)
                    {
                        UnRead(bytes);
                        if (ThrowOnTimeout) throw new TimeoutException();
                    }
                }
                else
                {
                    UnRead(bytes[(i + 1)..]);
                }
            }
            return cp;

            void DoReadByte(Span<byte> bytes, ref TimeSpan timeout, ref int totalReadCount)
            {
                if (totalReadCount >= bytes.Length) return;
                if (timeout == Timeout.InfiniteTimeSpan)
                {
                    var r = ReadByte();
                    if (r != -1)
                    {
                        bytes[totalReadCount++] = (byte)r;
                    }
                    return;
                }

                try
                {
                    var beg = _readByteTimeoutSum;
                    totalReadCount += ReadBytes(bytes[totalReadCount..], timeout, false);
                    var readTime = _readByteTimeoutSum - beg;
                    timeout = readTime > timeout ? TimeSpan.Zero : timeout - readTime;
                }
                catch (TimeoutException) { }
            }
        }

        public int ReadCodePoints(Span<int> sp, bool tryFill = true)
        {
            return ReadCodePoints(sp, Timeout.InfiniteTimeSpan, tryFill);
        }

        public int ReadCodePoints(Span<int> sp, TimeSpan timeout, bool tryFill = true)
        {
            ThrowIfDisposed();

            int readCount = 0;
            try
            {
                var accum = TimeSpan.Zero;
                for (; readCount < sp.Length; readCount++)
                {
                    var beg = _readByteTimeoutSum;
                    int ch = ReadCodePoint(timeout - accum);
                    if (ch == -1) break;
                    sp[readCount] = ch;
                    accum += _readByteTimeoutSum - beg;
                    if (timeout != Timeout.InfiniteTimeSpan)
                    {
                        if (accum >= timeout) accum = timeout;
                    }
                }
            }
            catch (TimeoutException)
            {
                if (readCount <= 0) throw;
            }
            return readCount;
        }

        private TimeSpan _graphemeTimeoutSum = TimeSpan.Zero;

        public Grapheme? ReadGrapheme() => ReadGrapheme(Timeout.InfiniteTimeSpan);
        public Grapheme? ReadGrapheme(TimeSpan timeout)
        {
            var builder = new GraphemeBuilder();

            Span<char> storage = stackalloc char[8 * 2];
            var list = new SlimList<char>(storage);

            while (true)
            {
                var cp = -1;
                var begTime = _readByteTimeoutSum;
                try
                {
                    cp = ReadCodePoint(timeout);
                }
                catch (TimeoutException) { }

                var endTime = _readByteTimeoutSum;

                if (cp == -1) break;

                builder.Add(new(cp));

                {
                    list.EnsureCapacity(builder.Length * 2);

                    var sp = list.AsUnsafeSpan();

                    var count = ((ReadOnlySpan<int>)[cp]).FromUtf32String(sp[list.Length..]);
                    Debug.Assert(count > 0);

                    list.UnsafeSetLength(list.Length + count);
                }

                var realSp = list.AsSpan();
                var firstLength = StringInfo.GetNextTextElementLength(realSp);
                if (firstLength < realSp.Length)
                {
                    break;
                }

                if (timeout != Timeout.InfiniteTimeSpan)
                {
                    var elapsed = endTime - begTime;

                    if (timeout != TimeSpan.Zero)
                        _graphemeTimeoutSum += elapsed;

                    timeout = elapsed > timeout ? TimeSpan.Zero : timeout - elapsed;
                }
            }

            // NOTE: Here, we assume that if a grapheme cluster came in, then it must have arrived
            // at the same time (or at least by the time the timeout expires)
            return DoRest(ref builder, list.AsSpan());

            Grapheme? DoRest(ref GraphemeBuilder builder, Span<char> span)
            {
                if (builder.Length <= 0 || span.Length <= 0)
                {
                    if (ThrowOnTimeout && !IsEof) throw new TimeoutException();
                    return null;
                }
                var firstLength = StringInfo.GetNextTextElementLength(span);
                if (firstLength < span.Length || IsEof)
                {
                    return BuildGrapheme(ref builder, span);
                }

                // NOTE: Don't rollback if only potentially incomplete; we shall assume all of a grapheme's code points
                // come in at the same time (otherwise, some keys e.g. \r can't get read until we type something else).
                //
                // if (GraphemeUtils.IsIncomplete(builder.UnsafeRunes, IsEof))
                // {
                //     if (!IsEof)
                //         UnRead(span);
                //     return null;
                // }

                if (GraphemeUtils.IsIncomplete(builder.UnsafeRunes, true))
                {
                    if (!IsEof)
                        UnRead(span);
                    return null;
                }

                return BuildGrapheme(ref builder, span);

                Grapheme BuildGrapheme(ref GraphemeBuilder builder, Span<char> span)
                {
                    var firstCpCount = span[..firstLength].CodePointCount();
                    UnRead(span[firstLength..]);
                    builder.RemoveRange(firstCpCount..);
                    return builder.AsGrapheme();
                }
            }
        }

        public int ReadGraphemes(Span<Grapheme> sp, TimeSpan timeout, bool tryFill = true)
        {
            int readCount = 0;
            try
            {
                var accum = TimeSpan.Zero;
                for (; readCount < sp.Length; readCount++)
                {
                    var beg = _readByteTimeoutSum;
                    var g = ReadGrapheme(timeout - accum);
                    if (g is null) break;
                    sp[readCount] = g.Value;
                    accum += _readByteTimeoutSum - beg;
                    if (timeout != Timeout.InfiniteTimeSpan)
                    {
                        if (accum >= timeout) accum = timeout;
                    }
                }
            }
            catch (TimeoutException)
            {
                if (readCount <= 0) throw;
            }
            return readCount;
        }

        /// <summary>
        /// Inserts the bytes into the stream such that the next <c>Read</c> call reads them.
        /// Note that this inserts items into a buffer which is ahead of the actual underlying stream.
        /// As such, inserting with <c>front = false</c> does NOT place data for it to be read after the stream EOFs (it
        /// is placed at the back of a buffer which is emptyied BEFORE continuing reading on the underlying stream).
        /// </summary>
        /// <param name="bytes">The bytes to insert.</param>
        /// <param name="front">Whether to place the items at the front or back of the stream. If placed at the front,
        /// bytes[0] will be at the head of the read queue. If placed at the back, bytes[^1] will be at the end of the
        /// read queue.</param>
        public void UnRead(ReadOnlySpan<byte> bytes, bool front = true)
        {
            if (bytes.Length <= 0) return;
            _stream.UnRead(bytes, front: front);
        }

        /// <summary>
        /// Inserts the chars (encoded into bytes) into the stream such that the next <c>Read</c> call reads them.
        /// Note that this inserts items into a buffer which is ahead of the actual underlying stream.
        /// As such, inserting with <c>front = false</c> does NOT place data for it to be read after the stream EOFs (it
        /// is placed at the back of a buffer which is emptyied BEFORE continuing reading on the underlying stream).
        /// </summary>
        /// <param name="chars">The chars to insert. Note that these will be encoded into bytes before being
        /// inserted.</param>
        /// <param name="front">Whether to place the items at the front or back of the stream. If placed at the front,
        /// bytes[0] will be at the head of the read queue. If placed at the back, bytes[^1] will be at the end of the
        /// read queue.</param>
        public void UnRead(ReadOnlySpan<char> chars, bool front = true)
        {
            if (chars.Length <= 0) return;

            int maxCount = _encoding.GetMaxByteCount(chars.Length);
            if (maxCount <= 0) return;

            const int POOL_THRESHOLD = 1024;

            if (maxCount >= POOL_THRESHOLD)
            {
                using var bytes = ArrayPool<byte>.Shared.AutoRent(maxCount);
                DoUnread(chars, bytes, front);
            }
            else
            {
                Span<byte> bytes = stackalloc byte[maxCount];
                DoUnread(chars, bytes, front);
            }

            void DoUnread(ReadOnlySpan<char> chars, Span<byte> bytes, bool front)
            {
                int count = _encoding.GetBytes(chars, bytes);
                if (count <= 0) return;
                UnRead(bytes[..count], front);
            }
        }

        /// <summary>
        /// Inserts the code points (encoded into bytes) into the stream such that the next <c>Read</c> call reads them.
        /// Note that this inserts items into a buffer which is ahead of the actual underlying stream.
        /// As such, inserting with <c>front = false</c> does NOT place data for it to be read after the stream EOFs (it
        /// is placed at the back of a buffer which is emptied BEFORE continuing reading on the underlying stream).
        /// </summary>
        /// <param name="codePoints">The code points to insert. Note that these will be encoded into bytes before being
        /// inserted.</param>
        /// <param name="front">Whether to place the items at the front or back of the stream (buffer). If placed at the front,
        /// bytes[0] will be at the head of the read queue. If placed at the back, bytes[^1] will be at the end of the
        /// read queue.</param>
        public void UnRead(ReadOnlySpan<int> codePoints, bool front = true)
        {
            if (codePoints.Length <= 0) return;

            var maxCharCount = Encodings.UTF32.GetMaxCharCount(codePoints.Length * sizeof(int));
            if (maxCharCount <= 0) return;

            const int POOL_THRESHOLD = 1024;

            if (maxCharCount >= POOL_THRESHOLD)
            {
                using var chars = ArrayPool<char>.Shared.AutoRent(maxCharCount);
                DoUnread(codePoints, chars, front);
            }
            else
            {
                Span<char> chars = stackalloc char[maxCharCount];
                DoUnread(codePoints, chars, front);
            }

            void DoUnread(ReadOnlySpan<int> codePoints, Span<char> chars, bool front)
            {
                int decodeCount = 0;
                if (BitConverter.IsLittleEndian)
                {
                    decodeCount = Encodings.UTF32LE.GetChars(MemoryMarshal.AsBytes(codePoints), chars);
                }
                else
                {
                    decodeCount = Encodings.UTF32BE.GetChars(MemoryMarshal.AsBytes(codePoints), chars);
                }
                if (decodeCount <= 0) return;
                UnRead(chars[..decodeCount], front);
            }
        }

        /// <summary>
        /// Inserts the code point (encoded into bytes) into the stream such that the next <c>Read</c> call reads them.
        /// Note that this inserts the item into a buffer which is ahead of the actual underlying stream.
        /// As such, inserting with <c>front = false</c> does NOT place data for it to be read after the stream EOFs (it
        /// is placed at the back of a buffer which is emptyied BEFORE continuing reading on the underlying stream).
        /// </summary>
        /// <param name="codePoint">The code point to insert. Note that this will be encoded into bytes before being
        /// inserted.</param>
        /// <param name="front">Whether to place the item at the front or back of the stream. If placed at the front,
        /// bytes[0] will be at the head of the read queue. If placed at the back, bytes[^1] will be at the end of the
        /// read queue.</param>
        public void UnRead(int codePoint, bool front = true)
        {
            Span<int> buf = [codePoint];
            UnRead(buf, front);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _stream.Dispose();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            await _stream.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
        }
    }
}
