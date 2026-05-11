// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;

namespace Sphynx.Collections
{

    /// <summary>
    /// ref struct backed by a caller-supplied Span{T} for initial storage.
    /// Spills to a heap array transparently once that span is exhausted.
    /// Cannot implement IList{T} because ref structs cannot be boxed.
    /// foreach is supported via duck-typed GetEnumerator().
    /// </summary>
    public ref struct SlimList<T>
    {
        private readonly Span<T> _initial; // caller-supplied initial storage
        private T[]? _array; // non-null once we spill to the heap
        private int _length;

        /// <param name="initialStorage">
        /// Span to use as the inline buffer before any heap allocation.
        /// Typically a stackalloc'd buffer or a pooled array slice.
        /// </param>
        public SlimList(Span<T> initialStorage)
        {
            _initial = initialStorage;
            _array = null;
            _length = 0;
        }

        public readonly int Length => _length;
        public readonly int Capacity => _array?.Length ?? _initial.Length;
        public readonly bool IsEmpty => _length == 0;
        public readonly bool OnHeap => _array is not null;

        public readonly ref T this[Index i] => ref this[i.GetOffset(Length)];
        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_length) ThrowOutOfRange();
                return ref Storage[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            int len = _length;
            if (len == Capacity) Grow(len + 1);
            Storage[len] = item;
            _length = len + 1;
        }

        public bool TryAdd(T item)
        {
            if (_length == Capacity) return false;
            Storage[_length++] = item;
            return true;
        }

        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)_length) ThrowOutOfRange();
            if (_length == Capacity) Grow(_length + 1);
            if (index < _length)
                Storage[index.._length].CopyTo(Storage[(index + 1)..]);
            Storage[index] = item;
            _length++;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_length) ThrowOutOfRange();
            Storage[(index + 1).._length].CopyTo(Storage[index..]);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                Storage[_length - 1] = default!;
            _length--;
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                Storage[.._length].Clear();
            _length = 0;
        }

        public void EnsureCapacity(int capacity)
        {
            if (capacity > Capacity) Grow(capacity);
        }

        public readonly int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
                if (comparer.Equals(span[i], item))
                    return i;
            return -1;
        }

        public readonly bool Contains(T item) => IndexOf(item) >= 0;

        public readonly void CopyTo(T[] array, int arrayIndex)
            => AsSpan().CopyTo(array.AsSpan(arrayIndex));

        /// <summary>
        /// Span over live elements [0..Length).
        ///
        /// Inline path: points into the caller-supplied initial storage.
        /// Do not mutate or invalidate that storage while this span is live.
        /// </summary>
        public readonly Span<T> AsSpan() => Storage[.._length];

        public readonly Span<T> AsUnsafeSpan() => Storage;

        public void UnsafeSetLength(int length)
        {
            if ((uint)length > (uint)Capacity) ThrowLengthExceedsCapacity();
            _length = length;
        }

        /// <summary>
        /// Snapshots AsSpan() at the moment of the call.
        /// Mutations to the list after GetEnumerator() do not affect iteration.
        /// </summary>
        public readonly Span<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

        // Whichever backing store is currently active, as a full-capacity span.
        private readonly Span<T> Storage
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array is not null ? _array.AsSpan() : _initial;
        }

        private void Grow(int minimum)
        {
            // Double from current capacity, but at least satisfy the minimum.
            // If _initial is empty (default span), start from 4.
            int current = Capacity;
            if (minimum <= current) return;
            int newCap = Math.Max(minimum, current == 0 ? 4 : current * 2);
            T[] newArray = GC.AllocateUninitializedArray<T>(newCap);
            Storage[.._length].CopyTo(newArray);
            _array = newArray;
        }

        private static void ThrowOutOfRange() =>
            throw new IndexOutOfRangeException();

        private static void ThrowLengthExceedsCapacity() =>
            throw new ArgumentOutOfRangeException(
                "length",
                "Length exceeds current capacity.");
    }

    /// <summary>A SlimList{T} which cannot grow past the span. Throws or returns false on overflow.</summary>
    public ref struct FixedSlimList<T>
    {
        private readonly Span<T> _storage;
        private int _length;

        public FixedSlimList(Span<T> storage)
        {
            _storage = storage;
            _length = 0;
        }

        public readonly int Length => _length;
        public readonly int Capacity => _storage.Length;
        public readonly bool IsEmpty => _length == 0;
        public readonly bool IsFull => _length == _storage.Length;

        public readonly ref T this[Index i] => ref this[i.GetOffset(Length)];

        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_length) ThrowOutOfRange();
                return ref _storage[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            int len = _length;
            if (len == _storage.Length) ThrowFull();
            _storage[len] = item;
            _length = len + 1;
        }

        public bool TryAdd(T item)
        {
            if (_length == _storage.Length) return false;
            _storage[_length++] = item;
            return true;
        }

        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)_length) ThrowOutOfRange();
            if (_length == _storage.Length) ThrowFull();
            if (index < _length)
                _storage[index.._length].CopyTo(_storage[(index + 1)..]);
            _storage[index] = item;
            _length++;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_length) ThrowOutOfRange();
            _storage[(index + 1).._length].CopyTo(_storage[index..]);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                _storage[_length - 1] = default!;
            _length--;
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                _storage[.._length].Clear();
            _length = 0;
        }

        public readonly void EnsureCapacity(int capacity)
        {
            if (capacity > _storage.Length)
                throw new InvalidOperationException(
                    $"FixedSlimList storage is fixed at {_storage.Length} elements; " +
                    $"cannot grow to {capacity}.");
        }

        public readonly int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _length; i++)
                if (comparer.Equals(_storage[i], item))
                    return i;
            return -1;
        }

        public readonly bool Contains(T item) => IndexOf(item) >= 0;

        public readonly void CopyTo(T[] array, int arrayIndex)
            => AsSpan().CopyTo(array.AsSpan(arrayIndex));

        /// <summary>Span over live elements [0..Length).</summary>
        public readonly Span<T> AsSpan() => _storage[.._length];

        public readonly Span<T> AsUnsafeSpan() => _storage;

        // Called by FixedSlimListUpdater.Dispose.
        public void SetLength(int length)
        {
            if ((uint)length > (uint)_storage.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    "Length exceeds storage capacity.");
            _length = length;
        }

        public readonly Span<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

        private static void ThrowOutOfRange() =>
            throw new IndexOutOfRangeException();

        private static void ThrowFull() =>
            throw new InvalidOperationException("FixedSlimList is full.");
    }

}
