// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sphynx.Collections
{
    /// <summary>
    /// A double-ended queue (deque) backed by a circular array buffer.
    /// Not thread-safe.
    /// </summary>
    public sealed class Deque<T> : IDeque<T>
    {
        private static readonly bool IsRefType = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        private T[] _buf;
        private int _head;
        private const int DEFAULT_CAPACITY = 8;

        public Deque() => _buf = new T[DEFAULT_CAPACITY];

        public Deque(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative for Deque");
            _buf = new T[Math.Max(capacity, 1)];
        }

        public Deque(IEnumerable<T> collection)
        {
            switch (collection)
            {
                case T[] arr:
                    _buf = new T[Math.Max(arr.Length, DEFAULT_CAPACITY)];
                    arr.AsSpan().CopyTo(_buf);
                    Count = arr.Length;
                    break;
                case List<T> list:
                    _buf = new T[Math.Max(list.Count, DEFAULT_CAPACITY)];
                    CollectionsMarshal.AsSpan(list).CopyTo(_buf);
                    Count = list.Count;
                    break;
                default:
                    _buf = new T[DEFAULT_CAPACITY];
                    foreach (var item in collection) EnqueueBack(item);
                    break;
            }
        }

        public Deque(ReadOnlySpan<T> span)
        {
            _buf = new T[Math.Max(span.Length, DEFAULT_CAPACITY)];
            span.CopyTo(_buf);
            Count = span.Length;
        }

        public int Count { get; private set; }
        public int Capacity => _buf.Length;

        public void EnsureCapacity(int minimum)
        {
            if (_buf.Length < minimum) Grow(minimum);
        }

        public void TrimExcess()
        {
            if (Count == 0)
            {
                if (_buf.Length != 1)
                    _buf = new T[1];
                _head = 0;
                return;
            }
            if (Count == _buf.Length) return;
            Reallocate(Count);
        }

        public void EnqueueBack(T item)
        {
            GrowIfFull();
            _buf[Tail] = item;
            Count++;
        }

        public void EnqueueFront(T item)
        {
            GrowIfFull();
            _head = Dec(_head);
            _buf[_head] = item;
            Count++;
        }

        public T DequeueBack()
        {
            ThrowIfEmpty();
            int idx = PhysicalIndex(Count - 1);
            T item = _buf[idx];
            ClearSlot(idx);
            Count--;
            return item;
        }

        public T DequeueFront()
        {
            ThrowIfEmpty();
            T item = _buf[_head];
            ClearSlot(_head);
            _head = Inc(_head);
            Count--;
            return item;
        }

        public bool TryDequeueFront([MaybeNullWhen(false)] out T item)
        {
            if (Count == 0) { item = default; return false; }
            item = DequeueFront();
            return true;
        }

        public bool TryDequeueBack([MaybeNullWhen(false)] out T item)
        {
            if (Count == 0) { item = default; return false; }
            item = DequeueBack();
            return true;
        }

        public T PeekFront()
        {
            ThrowIfEmpty();
            return _buf[_head];
        }

        public T PeekBack()
        {
            ThrowIfEmpty();
            return _buf[PhysicalIndex(Count - 1)];
        }

        public bool TryPeekFront([MaybeNullWhen(false)] out T item)
        {
            if (Count == 0) { item = default; return false; }
            item = _buf[_head];
            return true;
        }

        public bool TryPeekBack([MaybeNullWhen(false)] out T item)
        {
            if (Count == 0) { item = default; return false; }
            item = _buf[PhysicalIndex(Count - 1)];
            return true;
        }

        /// <summary>Appends a span to the back via two-segment block copy.</summary>
        public void EnqueueBack(ReadOnlySpan<T> items, bool reverse = false)
        {
            if (items.IsEmpty) return;
            EnsureCapacity(Count + items.Length);
            if (!reverse)
            {
                TwoSegCopyIn(Count, items);
            }
            else
            {
                for (int i = 0; i < items.Length; i++)
                    _buf[PhysicalIndex(Count + i)] = items[items.Length - 1 - i];
            }
            Count += items.Length;
        }

        /// <summary>Appends items to the back in enumeration order.</summary>
        public void EnqueueBack(IEnumerable<T> items, bool reverse = false)
        {
            switch (items)
            {
                case T[] arr:
                    EnqueueBack(arr.AsSpan(), reverse);
                    break;
                case List<T> list:
                    EnqueueBack(CollectionsMarshal.AsSpan(list), reverse);
                    break;
                case ICollection<T> col:
                    EnsureCapacity(Count + col.Count);
                    goto default;
                default:
                    foreach (var item in reverse ? items.Reverse() : items) EnqueueBack(item);
                    break;
            }
        }

        /// <summary>
        /// Prepends a span to the front via two-segment block copy.
        /// When <paramref name="reverse"/> is true (default), items[0] ends up at the new front.
        /// </summary>
        public void EnqueueFront(ReadOnlySpan<T> items, bool reverse = true)
        {
            if (items.IsEmpty) return;
            EnsureCapacity(Count + items.Length);
            _head = Wrap(_head - items.Length);
            Count += items.Length;
            if (reverse)
            {
                TwoSegCopyIn(0, items);
            }
            else
            {
                for (int i = 0; i < items.Length; i++)
                    _buf[PhysicalIndex(i)] = items[items.Length - 1 - i];
            }
        }

        /// <summary>Prepends items to the front in enumeration order (items[0] at front).</summary>
        public void EnqueueFront(IEnumerable<T> items, bool reverse = true)
        {
            switch (items)
            {
                case T[] arr:
                    EnqueueFront(arr.AsSpan(), reverse);
                    break;
                case List<T> list:
                    EnqueueFront(CollectionsMarshal.AsSpan(list), reverse);
                    break;
                default:
                    var list2 = new List<T>(items);
                    EnqueueFront(CollectionsMarshal.AsSpan(list2), reverse);
                    break;
            }
        }

        /// <summary>Dequeues up to <c>storage.Length</c> items from the front. Returns items written.</summary>
        public int DequeueFront(Span<T> storage, bool reverse = false)
        {
            if (storage.IsEmpty || Count == 0) return 0;
            int n = Math.Min(Count, storage.Length);
            TwoSegCopyOut(0, n, storage);
            TwoSegClear(0, n);
            _head = Wrap(_head + n);
            Count -= n;
            if (reverse) storage[..n].Reverse();
            return n;
        }

        /// <summary>Dequeues up to <paramref name="count"/> items from the front.</summary>
        public List<T> DequeueFront(int count, bool reverse = false)
        {
            if (count <= 0 || Count == 0) return new List<T>(0);
            int n = Math.Min(Count, count);
            var result = new List<T>(n);
            result.AddRange(Enumerable.Repeat<T>(default!, n));
            DequeueFront(CollectionsMarshal.AsSpan(result), reverse);
            return result;
        }

        /// <summary>Dequeues up to <c>storage.Length</c> items from the back. Returns items written.</summary>
        public int DequeueBack(Span<T> storage, bool reverse = true)
        {
            if (storage.IsEmpty || Count == 0) return 0;
            int n = Math.Min(Count, storage.Length);
            int logicalStart = Count - n;
            TwoSegCopyOut(logicalStart, n, storage);
            TwoSegClear(logicalStart, n);
            Count -= n;
            if (!reverse) storage[..n].Reverse();
            return n;
        }

        /// <summary>Dequeues up to <paramref name="count"/> items from the back.</summary>
        public List<T> DequeueBack(int count, bool reverse = true)
        {
            if (count <= 0 || Count == 0) return new List<T>(0);
            int n = Math.Min(Count, count);
            var result = new List<T>(n);
            result.AddRange(Enumerable.Repeat<T>(default!, n));
            DequeueBack(CollectionsMarshal.AsSpan(result), reverse);
            return result;
        }

        public T this[int index]
        {
            get { CheckIndex(index); return _buf[PhysicalIndex(index)]; }
            set { CheckIndex(index); _buf[PhysicalIndex(index)] = value; }
        }

        public T this[Index index] => this[index.GetOffset(Count)];

        public bool Contains(T item)
        {
            var cmp = EqualityComparer<T>.Default;
            for (int i = 0; i < Count; i++)
                if (cmp.Equals(_buf[PhysicalIndex(i)], item)) return true;
            return false;
        }

        public void Clear()
        {
            TwoSegClear(0, Count);
            _head = 0;
            Count = 0;
        }

        public void Rotate(int steps)
        {
            if (Count <= 1) return;
            steps = ((steps % Count) + Count) % Count;
            if (steps == 0) return;
            // three-reversal O(n) but correct
            AsSpan()[..steps].Reverse();
            AsSpan()[steps..].Reverse();
            AsSpan().Reverse();
        }

        public void Reverse() => AsSpan().Reverse();

        private Span<T> AsSpan()
        {
            FlattenIfWrapped();
            return _buf.AsSpan(_head, Count);
        }

        public void Sort(IComparer<T>? comparer = null)
        {
            FlattenIfWrapped();
            _buf.AsSpan(_head, Count).Sort(comparer ?? Comparer<T>.Default);
        }

        public Deque<T> ToSorted(IComparer<T>? comparer = null)
        {
            var c = Clone();
            c.Sort(comparer);
            return c;
        }

        public Deque<T> Clone()
        {
            var d = new Deque<T>(Count);
            TwoSegCopyOut(0, Count, d._buf.AsSpan());
            d.Count = Count;
            return d;
        }

        public T[] ToArray()
        {
            if (Count == 0) return Array.Empty<T>();
            var arr = new T[Count];
            TwoSegCopyOut(0, Count, arr.AsSpan());
            return arr;
        }

        public List<T> ToList()
        {
            var list = new List<T>(Count);
            list.AddRange(Enumerable.Repeat<T>(default!, Count));
            TwoSegCopyOut(0, Count, CollectionsMarshal.AsSpan(list));
            return list;
        }

        public void CopyTo(Span<T> destination) => TwoSegCopyOut(0, Count, destination);

        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            if ((uint)arrayIndex > (uint)array.Length || arrayIndex + Count > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            TwoSegCopyOut(0, Count, array.AsSpan(arrayIndex));
        }

        public Deque<T> Filter(Func<T, bool> predicate)
        {
            var r = new Deque<T>();
            foreach (var item in this) if (predicate(item)) r.EnqueueBack(item);
            return r;
        }

        public Deque<TResult> Map<TResult>(Func<T, TResult> selector)
        {
            var r = new Deque<TResult>(Count);
            foreach (var item in this) r.EnqueueBack(selector(item));
            return r;
        }

        public TAccumulate Aggregate<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> func)
        {
            foreach (var item in this) seed = func(seed, item);
            return seed;
        }

        public bool Any(Func<T, bool> predicate)
        {
            foreach (var item in this) if (predicate(item)) return true;
            return false;
        }

        public bool All(Func<T, bool> predicate)
        {
            foreach (var item in this) if (!predicate(item)) return false;
            return true;
        }

        public (Deque<T> Front, Deque<T> Back) Split(int index)
        {
            if ((uint)index > (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
            var front = new Deque<T>(index);
            var back = new Deque<T>(Count - index);
            for (int i = 0; i < index; i++) front.EnqueueBack(this[i]);
            for (int i = index; i < Count; i++) back.EnqueueBack(this[i]);
            return (front, back);
        }

        public Deque<T> Concat(Deque<T> other)
        {
            var r = new Deque<T>(Count + other.Count);
            r.EnqueueBack((IEnumerable<T>)this);
            r.EnqueueBack((IEnumerable<T>)other);
            return r;
        }

        public bool IsReadOnly => false;
        void ICollection<T>.Add(T item) => EnqueueBack(item);

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return _buf[PhysicalIndex(i)];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        void ICollection.CopyTo(Array array, int index)
        {
            if (array is T[] typed) { CopyTo(typed, index); return; }
            for (int i = 0; i < Count; i++) array.SetValue(this[i], index + i);
        }

        private void TwoSegCopyOut(int logicalStart, int n, Span<T> dst)
        {
            n = Math.Min(n, Count - logicalStart);
            if (n <= 0) return;
            int physStart = PhysicalIndex(logicalStart);
            int firstSeg = Math.Min(n, _buf.Length - physStart);
            _buf.AsSpan(physStart, firstSeg).CopyTo(dst);
            if (firstSeg < n)
                _buf.AsSpan(0, n - firstSeg).CopyTo(dst[firstSeg..]);
        }

        private void TwoSegCopyIn(int logicalStart, ReadOnlySpan<T> src)
        {
            if (src.IsEmpty) return;
            int physStart = PhysicalIndex(logicalStart);
            int firstSeg = Math.Min(src.Length, _buf.Length - physStart);
            src[..firstSeg].CopyTo(_buf.AsSpan(physStart));
            if (firstSeg < src.Length)
                src[firstSeg..].CopyTo(_buf.AsSpan(0));
        }

        private void TwoSegClear(int logicalStart, int n)
        {
            if (!IsRefType) return;
            n = Math.Min(n, Count - logicalStart);
            if (n <= 0) return;
            int physStart = PhysicalIndex(logicalStart);
            int firstSeg = Math.Min(n, _buf.Length - physStart);
            _buf.AsSpan(physStart, firstSeg).Clear();
            if (firstSeg < n)
                _buf.AsSpan(0, n - firstSeg).Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Wrap(int i) => ((i % _buf.Length) + _buf.Length) % _buf.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Inc(int i) => i + 1 >= _buf.Length ? 0 : i + 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Dec(int i) => i == 0 ? _buf.Length - 1 : i - 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int PhysicalIndex(int logical) => Wrap(_head + logical);

        private int Tail
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Wrap(_head + Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearSlot(int physIdx) { if (IsRefType) _buf[physIdx] = default!; }

        private void GrowIfFull() { if (Count == _buf.Length) Grow(_buf.Length * 2); }
        private void Grow(int minimum) => Reallocate(Math.Max(minimum, _buf.Length * 2));

        private void Reallocate(int newCapacity)
        {
            var newBuf = new T[newCapacity];
            TwoSegCopyOut(0, Count, newBuf.AsSpan());
            _buf = newBuf;
            _head = 0;
        }

        private void FlattenIfWrapped()
        {
            if (_head + Count > _buf.Length) Reallocate(_buf.Length);
        }

        private void ThrowIfEmpty()
        {
            if (Count == 0) throw new InvalidOperationException("Deque is empty.");
        }

        private void CheckIndex(int index)
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
        }

        public bool Remove(T item)
        {
            int idx = IndexOf(item);
            if (idx < 0) return false;
            RemoveAt(idx);
            return true;
        }

        private int IndexOf(T item)
        {
            var cmp = EqualityComparer<T>.Default;
            for (int i = 0; i < Count; i++)
                if (cmp.Equals(_buf[PhysicalIndex(i)], item)) return i;
            return -1;
        }

        private void RemoveAt(int index)
        {
            if (index == 0) { DequeueFront(); return; }
            if (index == Count - 1) { DequeueBack(); return; }
            if (index < Count / 2)
            {
                for (int i = index; i > 0; i--)
                    _buf[PhysicalIndex(i)] = _buf[PhysicalIndex(i - 1)];
                ClearSlot(_head);
                _head = Inc(_head);
            }
            else
            {
                for (int i = index; i < Count - 1; i++)
                    _buf[PhysicalIndex(i)] = _buf[PhysicalIndex(i + 1)];
                ClearSlot(PhysicalIndex(Count - 1));
            }
            Count--;
        }
        public override string ToString() => $"Deque<{typeof(T).Name}>[{string.Join(", ", this)}]";
    }
}
