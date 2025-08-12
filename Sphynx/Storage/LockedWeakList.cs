// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Sphynx.Storage
{
    // Copyright (c) 2024 ppy Pty Ltd <contact@ppy.sh>.
    //
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    //
    // The above copyright notice and this permission notice shall be included in
    // all copies or substantial portions of the Software.

    /// <summary>
    /// A <see cref="IWeakList{T}"/> which locks all operations.
    /// </summary>
    public class LockedWeakList<T> : IWeakList<T>, IEnumerable<T>
        where T : class
    {
        private readonly WeakList<T> list = new WeakList<T>();

        public void Add(T item)
        {
            lock (list)
                list.Add(item);
        }

        public void Add(WeakReference<T> weakReference)
        {
            lock (list)
                list.Add(weakReference);
        }

        public bool Remove(T item)
        {
            lock (list)
                return list.Remove(item);
        }

        public bool Remove(WeakReference<T> weakReference)
        {
            lock (list)
                return list.Remove(weakReference);
        }

        public void RemoveAt(int index)
        {
            lock (list)
                list.RemoveAt(index);
        }

        public bool Contains(T item)
        {
            lock (list)
                return list.Contains(item);
        }

        public bool Contains(WeakReference<T> weakReference)
        {
            lock (list)
                return list.Contains(weakReference);
        }

        public void Clear()
        {
            lock (list)
                list.Clear();
        }

        public Enumerator GetEnumerator() => new Enumerator(list);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private readonly WeakList<T> list;

            private WeakList<T>.ValidItemsEnumerator listEnumerator;

            private readonly bool lockTaken;

            internal Enumerator(WeakList<T> list)
            {
                this.list = list;

                lockTaken = false;
                Monitor.Enter(list, ref lockTaken);

                listEnumerator = list.GetEnumerator();
            }

            public bool MoveNext() => listEnumerator.MoveNext();

            public void Reset() => listEnumerator.Reset();

            public readonly T Current => listEnumerator.Current;

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                if (lockTaken)
                    Monitor.Exit(list);
            }
        }
    }
}
