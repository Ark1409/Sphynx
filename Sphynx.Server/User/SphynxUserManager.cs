using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using MongoDB.Bson.Serialization;
using Sphynx.Model.User;
using Sphynx.Network.Packet;
using Sphynx.Server.Storage;
using Sphynx.Server.Utils;
using Sphynx.Utils;

namespace Sphynx.Server.User
{
    public static class SphynxUserManager
    {
        private static readonly DatabaseStore<Guid, SphynxUserDbInfo> _userInfoStore;
        private static readonly string[] _ignoredUserFields = { SphynxUserDbInfo.PASSWORD_FIELD, SphynxUserDbInfo.PASSWORD_SALT_FIELD };

        public static event Action<SphynxUserDbInfo>? UserCreated;
        public static event Action<SphynxUserDbInfo>? UserDeleted;

        static SphynxUserManager()
        {
            using (var reader = new StreamReader(File.OpenRead(DatabaseInfoFile.NAME)))
            {
                reader.ReadLine();
                reader.ReadLine();
                string userCollectionName = reader.ReadLine()!;
                userCollectionName = "users";
                _userInfoStore = new MongoStore<SphynxUserDbInfo>(userCollectionName);
            }

            // BsonClassMap.RegisterClassMap<SphynxUserInfo>(cm =>
            // {
            //     cm.MapCreator(c => new SphynxUserInfo(default!, default!, default, default));
            //     cm.MapField("Id");
            // });
        }

        public static async Task<SphynxErrorInfo<SphynxUserDbInfo?>> CreateUserAsync(SphynxUserCredentials credentials)
        {
            // TODO: Make sure to index username
            if (await _userInfoStore.ContainsFieldAsync(credentials.UserName))
            {
                return new SphynxErrorInfo<SphynxUserDbInfo?>(SphynxErrorCode.INVALID_USERNAME);
            }

            // Hash password
            byte[] hashedPwd = PasswordManager.HashPassword(credentials.Password, out byte[] pwdSalt);

            // Create user
            var createdUser = new SphynxUserDbInfo(credentials.UserName, hashedPwd, pwdSalt, SphynxUserStatus.ONLINE);

            // Insert record for new user
            if (await _userInfoStore.InsertAsync(createdUser))
            {
                // Immediately null-out password
                createdUser.Password = null;
                createdUser.PasswordSalt = null;

                UserCreated?.Invoke(createdUser);
                return new SphynxErrorInfo<SphynxUserDbInfo?>(createdUser);
            }

            return new SphynxErrorInfo<SphynxUserDbInfo?>(SphynxErrorCode.DB_WRITE_ERROR);
        }

        public static async Task<SphynxErrorCode> DeleteUserAsync(Guid userId, string userPassword)
        {
            var dbUser = await _userInfoStore.GetAsync(userId);

            if (dbUser.ErrorCode != SphynxErrorCode.SUCCESS) return SphynxErrorCode.INVALID_USER;

            var passwordCheck = PasswordManager.VerifyPassword(dbUser.Data!.Password!, dbUser.Data.PasswordSalt!,
                userPassword);
            if (passwordCheck != SphynxErrorCode.SUCCESS) return passwordCheck;

            if (await _userInfoStore.DeleteAsync(userId))
            {
                UserDeleted?.Invoke(dbUser.Data!);
                return SphynxErrorCode.SUCCESS;
            }

            return SphynxErrorCode.DB_WRITE_ERROR;
        }

        public static Task<bool> UpdateUserAsync(SphynxUserDbInfo userInfo)
        {
            return _userInfoStore.UpdateAsync(userInfo);
        }

        public static Task<SphynxErrorInfo<SphynxUserDbInfo?>> GetUserAsync(Guid userId, bool includePassword = false)
        {
            return _userInfoStore.GetAsync(userId, includePassword ? _ignoredUserFields : Array.Empty<string>());
        }

        public static Task<SphynxErrorInfo<SphynxUserDbInfo?>> GetUserAsync(string userName, bool includePassword = false)
        {
            return _userInfoStore.GetWhereAsync(SphynxUserDbInfo.NAME_FIELD, userName,
                includePassword ? _ignoredUserFields : Array.Empty<string>());
        }

        public static Task<SphynxErrorInfo<TField?>> GetUserFieldAsync<TField>(Guid userId, string fieldName)
        {
            return _userInfoStore.GetFieldAsync<TField>(userId, fieldName);
        }

        public static Task<bool> UpdateUserFieldAsync<TField>(Guid userId, string fieldName, TField? value)
        {
            return _userInfoStore.PutFieldAsync(userId, fieldName, value);
        }
    }
}