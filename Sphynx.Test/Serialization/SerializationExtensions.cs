// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Sphynx.Core;

namespace Sphynx.Test.Serialization
{
    internal static class SerializationExtensions
    {
        public static Guid AsGuid(this string src)
        {
            if (string.IsNullOrEmpty(src))
                return Guid.Empty;

            byte[] md5 = MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(src));
            return new Guid(md5.AsSpan()[..Unsafe.SizeOf<Guid>()]);
        }

        public static SnowflakeId AsSnowflakeId(this string src)
        {
            if (string.IsNullOrEmpty(src))
                return SnowflakeId.Empty;

            byte[] md5 = MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(src));
            return new SnowflakeId(md5.AsSpan()[..SnowflakeId.SIZE]);
        }
    }
}
