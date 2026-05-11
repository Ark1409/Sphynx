// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;

namespace Sphynx.Collections
{
    public struct ViewedMemoryOwner<T> : IMemoryOwner<T>
    {
        private IMemoryOwner<T> _owner;
        private bool _disposed = false;

        public readonly Memory<T> Memory
        {
            get
            {
                ThrowIfDisposed();
                return _owner.Memory[_view];
            }
        }

        private Range _view = Range.All;
        public Range View
        {
            readonly get
            {
                ThrowIfDisposed();
                return _view;
            }

            set
            {
                _ = value.GetOffsetAndLength(_owner.Memory.Length);
                _view = value;
            }
        }

        public readonly int OriginalLength => _owner.Memory.Length;

        public ViewedMemoryOwner(IMemoryOwner<T> owner) => _owner = owner;
        public ViewedMemoryOwner(IMemoryOwner<T> owner, int length)
        {
            _owner = owner;
            Slice(0, length);
        }

        public ViewedMemoryOwner(T[]? array) : this(new PureMemoryOwner<T>(array)) { }
        public ViewedMemoryOwner(T[]? array, int start, int length) : this(new PureMemoryOwner<T>(array, start, length)) { }

        public void Slice(int start)
        {
            ThrowIfDisposed();
            var (_, len) = _view.GetOffsetAndLength(_owner.Memory.Length);
            Slice(start, len - start);
        }

        public void Slice(int start, int length)
        {
            ThrowIfDisposed();
            ArgumentOutOfRangeException.ThrowIfNegative(start);
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            var (off, len) = _view.GetOffsetAndLength(_owner.Memory.Length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, len);
            _view = new Range(off + start, off+start+length);
        }

        public void MoveStart(int diff)
        {
            var (_, len) = _view.GetOffsetAndLength(_owner.Memory.Length);
            Move(diff, len - diff);
        }
        public void MoveEnd(int diff)
        {
            var (_, len) = _view.GetOffsetAndLength(_owner.Memory.Length);
            Move(0, len + diff);
        }

        // Relative move
        private void Move(int start, int length)
        {
            ThrowIfDisposed();
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            var (off, _) = _view.GetOffsetAndLength(_owner.Memory.Length);
            ArgumentOutOfRangeException.ThrowIfNegative(start + off);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(start + off + length, _owner.Memory.Length);
            _view = new Range(off + start, off+start+length);
        }

        private readonly void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ViewedMemoryOwner<T>), $"Cannot use disposed {nameof(ViewedMemoryOwner<T>)}");
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner.Dispose();
            _owner = default!;
            GC.SuppressFinalize(this);
        }

        public static implicit operator ViewedMemoryOwner<T>(T[]? arr) => new(arr);
    }
}
