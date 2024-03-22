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
        // 

        private static readonly DatabaseStore<Guid, SphynxUserInfo> _userInfoStore;

        public static event Action<SphynxUserInfo>? UserCreated;
        public static event Action<SphynxUserInfo>? UserDeleted;

        static SphynxUserManager()
        {
            using (var reader = new StreamReader(File.OpenRead(DatabaseStoreFile.NAME)))
            {
                reader.ReadLine();
                reader.ReadLine();
                string userCollectionName = reader.ReadLine()!;
                _userInfoStore = new MongoStore<SphynxUserInfo>(userCollectionName);
            }

            BsonClassMap.RegisterClassMap<SphynxUserInfo>(cm =>
            {
                cm.MapCreator(c => new SphynxUserInfo(default!, default!, default, default));
                cm.MapField("Id");
            });
        }

        public static async Task<SphynxErrorInfo<SphynxUserInfo?>> CreateUserAsync(SphynxUserCredentials credentials)
        {
            if (await _userInfoStore.ContainsFieldAsync(credentials.Name))
            {
                return new SphynxErrorInfo<SphynxUserInfo?>(SphynxErrorCode.INVALID_USERNAME);
            }

            var newUser = new SphynxUserInfo(default, credentials.Name, SphynxUserStatus.ONLINE);

            if (await UpdateUserAsync(newUser))
            {
                UserCreated?.Invoke(newUser);
            }

            return new SphynxErrorInfo<SphynxUserInfo?>(newUser);
        }

        public static Task<bool> UpdateUserAsync(SphynxUserInfo userInfo)
        {
            return _userInfoStore.PutAsync(userInfo.UserId, userInfo);
        }

        public static Task<SphynxErrorInfo<SphynxUserInfo?>> GetUserAsync(Guid userId)
        {
            return _userInfoStore.GetAsync(userId);
        }

        public static Task<SphynxErrorInfo<T?>> GetUserFieldAsync<T>(Guid userId, string fieldName)
        {
            return _userInfoStore.GetFieldAsync<T>(userId, fieldName);
        }

        public static Task<bool> UpdateUserFieldAsync<T>(Guid userId, string fieldName, T? value)
        {
            return _userInfoStore.PutFieldAsync(userId, fieldName, value);
        }
        
        public static async Task<bool> DeleteUserAsync(Guid userId, SphynxUserCredentials credentials)
        {
            var userInfo = await GetUserAsync(userId);

            if (userInfo.ErrorCode == SphynxErrorCode.SUCCESS && await _userInfoStore.DeleteAsync(userId))
            {
                UserDeleted?.Invoke(userInfo.Data!);
                return true;
            }

            return false;
        }
    }
}