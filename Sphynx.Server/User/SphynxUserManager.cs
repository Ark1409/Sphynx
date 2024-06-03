using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using MongoDB.Bson.Serialization;
using Sphynx.Core;
using Sphynx.Packet;
using Sphynx.Server.Storage;
using Sphynx.Utils;

namespace Sphynx.Server.User
{
    public static class SphynxUserManager
    {
        // UserCollection - TryGetValue(UserId)
        // UserCollection - Put(UserId, UserInfo)
        //
        // RoomCollection - TryGetValue(RoomId)
        // RoomCollection - Put(RoomId, RoomInfo)
        //

        private static readonly DatabaseStore<Guid, SphynxDbUserInfo> _userInfoStore;
        private static readonly string[] _ignoredUserFields = new string[] { SphynxDbUserInfo.PASSWORD_FIELD, SphynxDbUserInfo.PASSWORD_SALT_FIELD };

        public static event Action<SphynxUserInfo>? UserCreated;
        public static event Action<SphynxUserInfo>? UserDeleted;

        static SphynxUserManager()
        {
            using (var reader = new StreamReader(File.OpenRead(DatabaseInfoFile.NAME)))
            {
                reader.ReadLine();
                reader.ReadLine();
                string userCollectionName = reader.ReadLine()!;
                userCollectionName = "users";
                _userInfoStore = new MongoStore<SphynxDbUserInfo>(userCollectionName);
            }

            // BsonClassMap.RegisterClassMap<SphynxUserInfo>(cm =>
            // {
            //     cm.MapCreator(c => new SphynxUserInfo(default!, default!, default, default));
            //     cm.MapField("Id");
            // });
        }

        public static async Task<SphynxErrorInfo<SphynxUserInfo?>> CreateUserAsync(SphynxUserCredentials credentials)
        {
            // TODO: Make sure to index username
            if (await _userInfoStore.ContainsFieldAsync(credentials.UserName))
            {
                return new SphynxErrorInfo<SphynxUserInfo?>(SphynxErrorCode.INVALID_USERNAME);
            }

            // Hash password
            byte[] hashedPwd = HashPassword(credentials.Password, out byte[] pwdSalt);

            try
            {
                // Create user
                var createdUser = new SphynxDbUserInfo(credentials.UserName, hashedPwd, pwdSalt, SphynxUserStatus.ONLINE);

                if (await _userInfoStore.InsertAsync(createdUser))
                {
                    UserCreated?.Invoke(createdUser);
                    // TODO: Perhaps null the password before returning
                    return new SphynxErrorInfo<SphynxUserInfo?>(createdUser);
                }

                return new SphynxErrorInfo<SphynxUserInfo?>(SphynxErrorCode.DB_WRITE_ERROR);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(pwdSalt);
            }
        }

        internal static byte[] HashPassword(string password, out byte[] rentedSalt)
        {
            // Since we are using SHA-256
            const int PWD_OUT_LEN = 256;
            RandomNumberGenerator.Fill(rentedSalt = ArrayPool<byte>.Shared.Rent(PWD_OUT_LEN));
            
            return HashPassword(password, rentedSalt);
        }

        internal static byte[] HashPassword(string password, byte[] rentedSalt)
        {
            // Standard amount
            const int PWD_ITERATIONS = 10_000;
            
            return Rfc2898DeriveBytes.Pbkdf2(password, rentedSalt, PWD_ITERATIONS, HashAlgorithmName.SHA256, rentedSalt.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool PasswordsEqual(byte[] password, byte[] otherPassword)
        {
            return new ReadOnlySpan<byte>(password).SequenceEqual(otherPassword);
        }

        internal static Task<bool> UpdateUserAsync(SphynxDbUserInfo userInfo)
        {
            return _userInfoStore.UpdateAsync(userInfo);
        }

        public static async Task<SphynxErrorInfo<SphynxUserInfo?>> GetUserAsync(Guid userId)
        {
            var dbUser = await _userInfoStore.GetAsync(userId, _ignoredUserFields);
            var shallowUser = new SphynxErrorInfo<SphynxUserInfo?>(dbUser.ErrorCode, dbUser.Data);
            return shallowUser;
        }
        
        public static async Task<SphynxErrorInfo<SphynxUserInfo?>> GetUserAsync(string userName)
        {
            var dbUser = await _userInfoStore.GetWhereAsync(SphynxUserInfo.NAME_FIELD, userName);
            var shallowUser = new SphynxErrorInfo<SphynxUserInfo?>(dbUser.ErrorCode, dbUser.Data);
            return shallowUser;
        }

        public static Task<SphynxErrorInfo<TField?>> GetUserFieldAsync<TField>(Guid userId, string fieldName)
        {
            return _userInfoStore.GetFieldAsync<TField>(userId, fieldName);
        }

        public static Task<bool> UpdateUserFieldAsync<TField>(Guid userId, string fieldName, TField? value)
        {
            return _userInfoStore.PutFieldAsync(userId, fieldName, value);
        }

        public static async Task<SphynxErrorCode> DeleteUserAsync(Guid userId, string password)
        {
            var dbUser = await _userInfoStore.GetAsync(userId);

            if (dbUser.ErrorCode != SphynxErrorCode.SUCCESS) return SphynxErrorCode.INVALID_USER;

            byte[] dbPwdSalt = Convert.FromBase64String(dbUser.Data!.PasswordSalt!);
            byte[] dbPwd = Convert.FromBase64String(dbUser.Data!.Password!);
            byte[] enteredPwd = HashPassword(password, dbPwdSalt);

            if (!PasswordsEqual(dbPwd, enteredPwd)) return SphynxErrorCode.INVALID_PASSWORD;

            if (await _userInfoStore.DeleteAsync(userId))
            {
                UserDeleted?.Invoke(dbUser.Data!);
                return SphynxErrorCode.SUCCESS;
            }

            return SphynxErrorCode.DB_WRITE_ERROR;
        }
    }
}