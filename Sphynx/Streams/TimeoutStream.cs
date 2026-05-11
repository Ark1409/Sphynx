// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using Sphynx.Collections;

namespace Sphynx.Streams
{
    public class TimeoutStream : Stream
    {
        private readonly Stream _stream;
        private readonly PureMemoryOwner<byte> _buffer;
        private ViewedMemoryOwner<byte> _currentStreamBuffer = default;
        private readonly Stopwatch _watch = new();
        private readonly ManualResetEventSlim _ev = new(false);
        private ValueTask<int> _currentStreamTask;
        private volatile bool _hasStreamTask = false;
        private int _disposed = 0;
        private readonly Action _readCompletionAction;

        private static readonly TimeoutException _timeoutException = new($"n-byte read timed out on {nameof(TimeoutStream)}");
        public bool ThrowOnTimeout { get; set; } = true;

        public TimeoutStream(Stream stream, int bufferSize = 1024)
        {
            _stream = stream;
            _buffer = new byte[bufferSize];
            _readCompletionAction = () =>
            {
                _watch.Stop();
                try
                {
                    var r = _currentStreamTask.Result;
                    _currentStreamBuffer.Slice(0, r);
                    _currentStreamTask = ValueTask.FromResult(r);
                }
                catch (OperationCanceledException e)
                {
                    _currentStreamTask = ValueTask.FromCanceled<int>(e.CancellationToken);
                }
                catch (Exception e)
                {
                    _currentStreamTask = ValueTask.FromException<int>(e);
                }
                _ev.Set();
            };
        }

        public bool IsEof { get; private set; } = false;
        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override bool CanTimeout => true;
        public override long Length => _stream.Length;

        public override long Position
        {
            get
            {
                var pos = _stream.Position;
                if (_hasStreamTask && _currentStreamTask.IsCompletedSuccessfully)
                {
                    pos -= _currentStreamBuffer.Memory.Length;
                }
                return pos;
            }

            set
            {
                if (!_hasStreamTask || !_currentStreamTask.IsCompletedSuccessfully)
                {
                    _stream.Position = value;
                    return;
                }

                var (off, len) = _currentStreamBuffer.View.GetOffsetAndLength(_currentStreamBuffer.OriginalLength);
                var diff = value - Position;
                if (-diff > off || len <= diff)
                {
                    ResetReadState();
                }
                else
                {
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(diff), int.MaxValue);
                    _currentStreamBuffer.MoveStart((int)diff);
                }
            }
        }
        public override int ReadTimeout { get; set; } = -1;
        private TimeSpan ReadTimeoutSpan => ReadTimeout < 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(ReadTimeout);
        public override int WriteTimeout { get => _stream.WriteTimeout; set => _stream.WriteTimeout = value; }

        public override int Read(byte[] buffer, int offset, int count) => DoRead(new Span<byte>(buffer, offset, count), ReadTimeoutSpan);
        public override int Read(Span<byte> buf) => DoRead(buf, ReadTimeoutSpan);

        public override int ReadByte()
        {
            Span<byte> b = stackalloc byte[1];
            var count = DoRead(b, ReadTimeoutSpan);
            return count <= 0 ? -1 : b[0];
        }

        // Reading here has NetworkStream semantics; it returns as soon as data is available.
        // That is, if you request X bytes, you can receive 0 <= Z <= X bytes. Left side is strict if timeout is
        // infinite and we're not at EOF.
        private int DoRead(Span<byte> buffer, TimeSpan timeout)
        {
            if (buffer.Length <= 0) return 0;

            // FIXME: Another read coming with no timeout will cause two reads to occur, though we may not even be supporting thread safety
            if (!_hasStreamTask && timeout == Timeout.InfiniteTimeSpan) return _stream.Read(buffer);

            EnsureReadStarted(buffer.Length);

            if (timeout != Timeout.InfiniteTimeSpan)
                timeout = timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout;

            return DoWait(ref buffer, ref timeout);

            int DoWait(ref Span<byte> buffer, ref TimeSpan timeout)
            {
                int readCount = 0;
                if (!_hasStreamTask) return readCount;

                var waitTime = WaitForRead(timeout);
                if (timeout != Timeout.InfiniteTimeSpan)
                    timeout -= waitTime;

                if (_ev.Wait(0))
                {
                    bool shouldReset = !_currentStreamTask.IsCompletedSuccessfully;
                    if (!shouldReset)
                    {
                        var mem = _currentStreamBuffer.Memory;
                        var useCount = Math.Min(mem.Length, buffer.Length);
                        IsEof = useCount <= 0;
                        if (!IsEof)
                        {
                            mem.Span[..useCount].CopyTo(buffer);
                            readCount += useCount;
                            buffer = buffer[useCount..];
                            shouldReset = useCount >= mem.Length;
                            if (!shouldReset) _currentStreamBuffer.Slice(useCount);
                        }
                    }

                    if (shouldReset)
                    {
                        ResetReadState();
                    }
                }
                else if (ThrowOnTimeout)
                {
                    throw _timeoutException;
                }

                return readCount;
            }
        }

        private void ResetReadState()
        {
            _ev.Reset();
            _hasStreamTask = false;
            _currentStreamTask = default;
            _currentStreamBuffer.Dispose();
            _currentStreamBuffer = default;
        }

        private bool EnsureReadStarted(int bufferSize, CancellationToken cancellationToken = default)
        {
            if (_hasStreamTask)
            {
                return false;
            }

            _ev.Reset();

            if (bufferSize <= _buffer.Memory.Length)
            {
                _currentStreamBuffer = new(_buffer);
            }
            else
            {
                var buffer = MemoryPool<byte>.Shared.Rent(bufferSize);
                _currentStreamBuffer = new(buffer);
            }

            _currentStreamTask = _stream.ReadAsync(_currentStreamBuffer.Memory, cancellationToken);
            _hasStreamTask = true;
            _currentStreamTask.GetAwaiter().OnCompleted(_readCompletionAction);

            return true;
        }

        private TimeSpan WaitForRead(TimeSpan timeout)
        {
            if (!_hasStreamTask) return TimeSpan.Zero;

            if (_ev.Wait(0)) return TimeSpan.Zero;

            if (timeout == Timeout.InfiniteTimeSpan)
            {
                _watch.Restart();
                _ev.Wait();
                _watch.Stop();
            }
            else
            {
                if (timeout <= TimeSpan.Zero) return TimeSpan.Zero;
                if (timeout <= TimeSpan.FromMilliseconds(2))
                {
                    var l = () => _ev.Wait(0);
                    _watch.Restart();
                    SpinWait.SpinUntil(l, timeout);
                    _watch.Stop();
                }
                else
                {
                    _watch.Restart();
                    _ev.Wait(timeout);
                    _watch.Stop();
                }
            }
            var res = _watch.Elapsed;
            return res > timeout ? timeout : res;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.End) return Fallback(offset, origin);

            if (_hasStreamTask || !_currentStreamTask.IsCompletedSuccessfully)
            {
                return Fallback(offset, origin);
            }

            var pos = Position;
            var (off, len) = _currentStreamBuffer.View.GetOffsetAndLength(_currentStreamBuffer.OriginalLength);

            switch (origin)
            {
                case SeekOrigin.Begin:
                {
                    var beg = pos - _currentStreamBuffer.OriginalLength;
                    if (offset >= beg && offset - beg < off + len)
                    {
                        ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(offset - beg - off), int.MaxValue);
                        _currentStreamBuffer.MoveStart((int)(offset - beg - off));
                        return beg + offset;
                    }
                    goto default;
                }
                case SeekOrigin.Current:
                    if (-offset <= off && offset < len)
                    {
                        ArgumentOutOfRangeException.ThrowIfGreaterThan(Math.Abs(offset), int.MaxValue);
                        _currentStreamBuffer.MoveStart((int)offset);
                        return pos + offset;
                    }
                    goto default;
                default:
                    return Fallback(offset, origin);
            }

            long Fallback(long offset, SeekOrigin origin)
            {
                long r = _stream.Seek(offset, origin);
                ResetReadState();
                return r;
            }
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buf) => _stream.Write(buf);
        public override void WriteByte(byte b) => _stream.WriteByte(b);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _stream.WriteAsync(buffer, cancellationToken);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _stream.WriteAsync(buffer, offset, count, cancellationToken);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => _stream.BeginWrite(buffer, offset, count, callback, state);
        public override void EndWrite(IAsyncResult asyncResult) => _stream.EndWrite(asyncResult);

        public override void Flush() => _stream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => _stream.FlushAsync(cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (_disposed >= 2) return;
            _disposed = disposing ? 2 : 1;
            if (disposing)
            {
                _stream.Dispose();
                if (_hasStreamTask)
                {
                    _currentStreamBuffer.Dispose();
                    _currentStreamBuffer = default;
                }
                _buffer.Dispose();
                _ev.Set();
            }
        }
    }
}
