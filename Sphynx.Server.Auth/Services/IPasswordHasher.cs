// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Security.Cryptography;

namespace Sphynx.Server.Auth.Services
{
    public interface IPasswordHasher
    {
        void HashPassword(ReadOnlySpan<char> password, ReadOnlySpan<byte> salt, Span<byte> destination);
        bool VerifyPassword(ReadOnlySpan<char> input, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> hashedPassword);
    }

    public static class PasswordHasherExtensions
    {
        private const int DEFAULT_HASH_LENGTH = 256;
        private const int DEFAULT_SALT_LENGTH = DEFAULT_HASH_LENGTH;

        public static byte[] GenerateSalt(this IPasswordHasher hasher, int length = DEFAULT_SALT_LENGTH)
        {
            return RandomNumberGenerator.GetBytes(length);
        }

        public static void GenerateSalt(this IPasswordHasher hasher, Span<byte> outSalt)
        {
            RandomNumberGenerator.Fill(outSalt);
        }

        public static byte[] HashPassword(this IPasswordHasher hasher, ReadOnlySpan<char> password, ReadOnlySpan<byte> salt)
        {
            byte[] dest = new byte[DEFAULT_HASH_LENGTH];
            hasher.HashPassword(password, salt, dest);
            return dest;
        }

        public static bool VerifyPassword(this IPasswordHasher hasher, ReadOnlySpan<char> input, ReadOnlySpan<char> encodedSalt, ReadOnlySpan<char> encodedPassword)
        {
            byte[] scratchArray = ArrayPool<byte>.Shared.Rent(encodedSalt.Length + encodedPassword.Length);
            var scratch = scratchArray.AsSpan();

            var saltSpan = scratch[..encodedSalt.Length];
            var passwordSpan = scratch[..encodedSalt.Length];

            try
            {
                if (!Convert.TryFromBase64Chars(encodedSalt, saltSpan, out _))
                    return false;

                if (!Convert.TryFromBase64Chars(encodedSalt, passwordSpan, out _))
                    return false;

                return hasher.VerifyPassword(input, saltSpan, passwordSpan);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(scratchArray);
            }
        }
    }
}
