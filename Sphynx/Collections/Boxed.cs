// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Collections
{
    public sealed class Boxed<T> where T : struct
    {
        public T Data;

        public Boxed(in T item)
        {
            Data = item;
        }

        public T Clone() => Data;
    }

    public static class BoxedExtensions
    {
        public static Boxed<T> Box<T>(this T item) where T : struct => new(in item);
    }
}
