// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Sphynx.Core;

namespace Sphynx.Test.Utils
{
    internal static class SerializationExtensions
    {
        private static readonly MD5 _md5 = MD5.Create();

        public static Guid AsGuid(this string src)
        {
            if (string.IsNullOrEmpty(src))
                return Guid.Empty;

            byte[] md5 = _md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(src));
            return new Guid(md5.AsSpan()[..Unsafe.SizeOf<Guid>()]);
        }

        public static SnowflakeId AsSnowflakeId(this string src)
        {
            if (string.IsNullOrEmpty(src))
                return SnowflakeId.Empty;

            byte[] md5 = _md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(src));
            return new SnowflakeId(md5.AsSpan()[..SnowflakeId.SIZE]);
        }

        public static Guid[] AsGuids(this string[] src)
        {
            var ids = new Guid[src.Length];

            for (int i = 0; i < src.Length; i++)
            {
                ids[i] = src[i].AsGuid();
            }

            return ids;
        }

        public static SnowflakeId[] AsSnowflakeIds(this string[] src)
        {
            var ids = new SnowflakeId[src.Length];

            for (int i = 0; i < src.Length; i++)
            {
                ids[i] = src[i].AsSnowflakeId();
            }

            return ids;
        }
    }
}
