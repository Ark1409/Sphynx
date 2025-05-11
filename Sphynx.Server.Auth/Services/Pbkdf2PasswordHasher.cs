// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Security.Cryptography;

namespace Sphynx.Server.Auth.Services
{
    public class Pbkdf2PasswordHasher : IPasswordHasher
    {
        private const int ITERATION_COUNT = 10_000;

        public void HashPassword(ReadOnlySpan<char> password, ReadOnlySpan<byte> salt, Span<byte> destination)
        {
            Rfc2898DeriveBytes.Pbkdf2(password, salt, destination, ITERATION_COUNT, HashAlgorithmName.SHA256);
        }

        public bool VerifyPassword(ReadOnlySpan<char> input, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> hashedPassword)
        {
            byte[]? hashArray = null;

            Span<byte> hash = hashedPassword.Length < 256
                ? stackalloc byte[256]
                : (hashArray = ArrayPool<byte>.Shared.Rent(hashedPassword.Length));

            try
            {
                HashPassword(input, salt, hash);
                return hash.SequenceEqual(hashedPassword);
            }
            finally
            {
                if (hashArray != null)
                    ArrayPool<byte>.Shared.Return(hashArray);
            }
        }
    }
}
