// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Collections
{
    /// <summary>
    /// A double-ended queue (deque) backed by a circular array buffer.
    /// </summary>
    public interface IDeque<T> : IReadOnlyList<T>, ICollection<T>, ICollection
    {
        int Capacity { get; }
        new int Count { get; }

        void EnqueueFront(T item);

        void EnqueueBack(T item);

        T DequeueFront();

        T DequeueBack();

        bool TryDequeueFront([MaybeNullWhen(false)] out T item);

        bool TryDequeueBack([MaybeNullWhen(false)] out T item);

        T PeekFront();

        T PeekBack();

        bool TryPeekFront([MaybeNullWhen(false)] out T item);

        bool TryPeekBack([MaybeNullWhen(false)] out T item);

        /// <summary>Appends a span to the back via two-segment block copy.</summary>
        void EnqueueBack(ReadOnlySpan<T> items, bool reverse = false);

        /// <summary>Appends items to the back in enumeration order.</summary>
        void EnqueueBack(IEnumerable<T> items, bool reverse = false);

        /// <summary>
        /// Prepends a span to the front via two-segment block copy.
        /// When <paramref name="reverse"/> is true (default), items[0] ends up at the new front.
        /// </summary>
        void EnqueueFront(ReadOnlySpan<T> items, bool reverse = true);

        /// <summary>Prepends items to the front in enumeration order (items[0] at front).</summary>
        void EnqueueFront(IEnumerable<T> items, bool reverse = true);

        /// <summary>Dequeues up to <c>storage.Length</c> items from the front.</summary>
        int DequeueFront(Span<T> storage, bool reverse = false);

        /// <summary>Dequeues up to <paramref name="count"/> items from the front.</summary>
        List<T> DequeueFront(int count, bool reverse = false);

        /// <summary>Dequeues up to <c>storage.Length</c> items from the back.</summary>
        int DequeueBack(Span<T> storage, bool reverse = true);

        /// <summary>Dequeues up to <paramref name="count"/> items from the back.</summary>
        List<T> DequeueBack(int count, bool reverse = true);

        bool ICollection<T>.IsReadOnly => false;

        /// <summary>
        /// Adds an item to the <see cref="ICollection{T}"/>. This adds the item to the end of the deque.
        /// </summary>
        /// <param name="item">The item to add.</param>
        void ICollection<T>.Add(T item) => EnqueueBack(item);

        int ICollection<T>.Count => Count;
        int IReadOnlyCollection<T>.Count => Count;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);

        T this[Index index] => this[index.GetOffset(Count)];
    }
}
