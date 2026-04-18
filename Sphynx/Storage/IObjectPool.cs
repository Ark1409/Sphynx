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

    public static class ObjectPoolExtensions
    {
        public static T Take<T>(this IObjectPool<T> pool)
            where T : class
        {
            if (!pool.TryTake(out var item))
                ThrowNoItemException();

            return item;

            [DoesNotReturn]
            static void ThrowNoItemException() => throw new InvalidOperationException();
        }
    }
}
