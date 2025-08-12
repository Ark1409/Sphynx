// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;

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

    public partial class WeakList<T>
    {
        /// <summary>
        /// An enumerator over only the valid items of a <see cref="WeakList{T}"/>.
        /// </summary>
        public struct ValidItemsEnumerator : IEnumerator<T>
        {
            private readonly WeakList<T> weakList;
            private int currentItemIndex;

            /// <summary>
            /// Creates a new <see cref="ValidItemsEnumerator"/>.
            /// </summary>
            /// <param name="weakList">The <see cref="WeakList{T}"/> to enumerate over.</param>
            internal ValidItemsEnumerator(WeakList<T> weakList)
            {
                this.weakList = weakList;

                currentItemIndex = weakList.listStart - 1; // The first MoveNext() should bring the iterator to the start
                Current = default!;
            }

            public bool MoveNext()
            {
                while (true)
                {
                    ++currentItemIndex;

                    // Check whether we're still within the valid range of the list.
                    if (currentItemIndex >= weakList.listEnd)
                        return false;

                    var weakReference = weakList.list[currentItemIndex].Reference;

                    // Check whether the reference exists.
                    if (weakReference == null || !weakReference.TryGetTarget(out var obj))
                    {
                        // If the reference doesn't exist, it must have previously been removed and can be skipped.
                        continue;
                    }

                    Current = obj;
                    return true;
                }
            }

            public void Reset()
            {
                currentItemIndex = weakList.listStart - 1;
                Current = default!;
            }

            public T Current { get; private set; }

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                Current = default!;
            }
        }
    }
}
