// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Utils
{
    public static class CollectionExtensions
    {
        public static CollectionRemover<T> CreateRemover<T>(this ICollection<T> coll)
        {
            return new(coll);
        }
    }

    public struct CollectionRemover<T> : IDisposable
    {
        private readonly ICollection<T> _coll;
        private readonly List<T> _items;

        public event Action<T>? OnRemoval = null;

        public CollectionRemover(ICollection<T> coll)
        {
            _coll = coll;
            _items = new(coll.Count / 2);
        }

        public void Execute()
        {
            foreach (var item in _items)
            {
                OnRemoval?.Invoke(item);
                _coll.Remove(item);
            }
            _items.Clear();
        }

        public void Enqueue(T item)
        {
            _items.Add(item);
        }

        public void Enqueue(Predicate<T> objPred)
        {
            foreach (var item in _coll)
            {
                if (objPred(item))
                {
                    Enqueue(item);
                }
            }
        }

        public void EnqueueFirst(Predicate<T> objPred)
        {
            foreach (var item in _coll)
            {
                if (objPred(item))
                {
                    Enqueue(item);
                    return;
                }
            }
        }

        public void Dispose()
        {
            Execute();
        }
    }
}
