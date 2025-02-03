// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Test.Utils
{
    internal static class CollectionUtils
    {
        public static bool AreEquivalent<T>(ICollection<T>? first, ICollection<T>? second)
        {
            if (first is null && second is null)
                return true;

            if (first is null || second is null)
                return false;

            return first.Count == second.Count && Is.EquivalentTo(first).ApplyTo(second).IsSuccess;
        }

        public static bool AreEqual<T>(ICollection<T>? first, ICollection<T>? second)
        {
            if (first is null && second is null)
                return true;

            if (first is null || second is null)
                return false;

            return first.Count == second.Count && Is.EqualTo(first).ApplyTo(second).IsSuccess;
        }
    }
}
