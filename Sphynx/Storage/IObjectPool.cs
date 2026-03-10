// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;

namespace Sphynx.Storage
{
    public interface IObjectPool<T> where T : class
    {
        bool TryTake([NotNullWhen(true)] out T? item);
        bool Return(T item);
        void Clear(Action<T>? clearAction = null);
    }
}
