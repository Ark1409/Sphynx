// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Sphynx.Server.Auth.Services
{
    public interface IPasswordHasher
    {
        // TODO: "ReadOnlySpan<byte>?"
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

        public static void GenerateSalt(this IPasswordHasher hasher, Span<byte> fill)
        {
            RandomNumberGenerator.Fill(fill);
        }

        public static byte[] HashPassword(this IPasswordHasher hasher, ReadOnlySpan<char> password, ReadOnlySpan<byte> salt)
        {
            byte[] dest = new byte[DEFAULT_HASH_LENGTH];
            hasher.HashPassword(password, salt, dest);
            return dest;
        }

        public static bool VerifyPassword(this IPasswordHasher hasher, ReadOnlySpan<char> input, ReadOnlySpan<char> encodedSalt,
            ReadOnlySpan<char> encodedPassword)
        {
            int passwordLength = GetBase64ByteCount(encodedSalt);
            int saltLength = GetBase64ByteCount(encodedSalt);

            byte[]? scratchArray = null!;
            var scratch = passwordLength + saltLength <= 512
                ? stackalloc byte[512]
                : ArrayPool<byte>.Shared.Rent(encodedSalt.Length + encodedPassword.Length);

            try
            {
                if (!Convert.TryFromBase64Chars(encodedSalt, scratch, out int bytesWritten))
                    return false;

                if (!Convert.TryFromBase64Chars(encodedSalt, scratch[bytesWritten..], out int saltBytesWritten))
                    return false;

                return hasher.VerifyPassword(input, scratch[bytesWritten..saltBytesWritten], scratch[..bytesWritten]);
            }
            finally
            {
                if (scratchArray != null)
                    ArrayPool<byte>.Shared.Return(scratchArray);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int GetBase64ByteCount(ReadOnlySpan<char> input)
        {
            fixed (char* inputPtr = input)
            {
                return GetBase64ByteCount(inputPtr, input.Length);
            }
        }

        // The MIT License (MIT)
        //
        // Copyright (c) .NET Foundation and Contributors
        //
        // All rights reserved.
        //
        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files (the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:
        //
        // The above copyright notice and this permission notice shall be included in all
        // copies or substantial portions of the Software.
        private static unsafe int GetBase64ByteCount(char* inputPtr, int inputLength)
        {
            const uint intEq = (uint)'=';
            const uint intSpace = (uint)' ';

            Debug.Assert(0 <= inputLength);

            char* inputEndPtr = inputPtr + inputLength;
            int usefulInputLength = inputLength;
            int padding = 0;

            while (inputPtr < inputEndPtr)
            {
                uint c = (uint)(*inputPtr);
                inputPtr++;

                // We want to be as fast as possible and filter out spaces with as few comparisons as possible.
                // We end up accepting a number of illegal chars as legal white-space chars.
                // This is ok: as soon as we hit them during actual decode we will recognise them as illegal and throw.
                if (c <= intSpace)
                    usefulInputLength--;
                else if (c == intEq)
                {
                    usefulInputLength--;
                    padding++;
                }
            }

            Debug.Assert(0 <= usefulInputLength);

            // For legal input, we can assume that 0 <= padding < 3. But it may be more for illegal input.
            // We will notice it at decode when we see a '=' at the wrong place.
            Debug.Assert(0 <= padding);

            // Perf: reuse the variable that stored the number of '=' to store the number of bytes encoded by the
            // last group that contains the '=':
            if (padding != 0)
            {
                if (padding == 1)
                    padding = 2;
                else if (padding == 2)
                    padding = 1;
                else
                    throw new FormatException($"Invalid Base64 string '{new string(inputPtr, 0, inputLength)}'");
            }

            // Done:
            return (usefulInputLength / 4) * 3 + padding;
        }
    }
}
