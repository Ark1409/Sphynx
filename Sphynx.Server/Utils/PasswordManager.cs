using System.Buffers;
using System.Security.Cryptography;
using Sphynx.Packet;
using Sphynx.Utils;

namespace Sphynx.Server.Utils
{
    internal static class PasswordManager
    {
        // Output length for Pbkdf2-generated key (password hash)
        internal const int PWD_HASH_LEN = 256;
        internal const int PWD_SALT_LEN = PWD_HASH_LEN;

        // Number of iterations of hashing
        internal const int PWD_HASH_ITERATIONS = 10_000;

        internal static byte[] HashPassword(ReadOnlySpan<char> password, out byte[] generatedSalt)
        {
            RandomNumberGenerator.Fill(generatedSalt = new byte[PWD_SALT_LEN]);
            return HashPassword(password, generatedSalt);
        }

        internal static void HashPassword(ReadOnlySpan<char> password, Span<byte> destination, out byte[] generatedSalt)
        {
            RandomNumberGenerator.Fill(generatedSalt = new byte[PWD_SALT_LEN]);
            HashPassword(password, generatedSalt, destination);
        }

        internal static byte[] HashPassword(ReadOnlySpan<char> password, ReadOnlySpan<byte> salt)
        {
            byte[] hashedPwd = new byte[PWD_HASH_LEN];
            HashPassword(password, salt, hashedPwd);
            return hashedPwd;
        }

        internal static void HashPassword(ReadOnlySpan<char> password, ReadOnlySpan<byte> salt, Span<byte> destination)
        {
            Rfc2898DeriveBytes.Pbkdf2(password, salt, destination, PWD_HASH_ITERATIONS, HashAlgorithmName.SHA256);
        }

        internal static SphynxErrorCode VerifyPassword(string dbPassword, string dbPasswordSalt, string enteredPassword)
        {
            byte[] dbPwd = ArrayPool<byte>.Shared.Rent(PWD_HASH_LEN);
            byte[] dbPwdSalt = ArrayPool<byte>.Shared.Rent(PWD_SALT_LEN);
            byte[] enteredPwd = ArrayPool<byte>.Shared.Rent(PWD_HASH_LEN);

            try
            {
                if (!Convert.TryFromBase64String(dbPassword, dbPwd, out _) ||
                    !Convert.TryFromBase64String(dbPasswordSalt, dbPwdSalt, out _))
                {
                    return SphynxErrorCode.DB_READ_ERROR;
                }

                HashPassword(enteredPassword, dbPwdSalt, enteredPwd);

                if (!MemoryUtils.SequenceEqual(dbPwd, enteredPwd)) return SphynxErrorCode.INVALID_PASSWORD;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(dbPwd);
                ArrayPool<byte>.Shared.Return(dbPwdSalt);
                ArrayPool<byte>.Shared.Return(enteredPwd);
            }

            return SphynxErrorCode.SUCCESS;
        }
    }
}