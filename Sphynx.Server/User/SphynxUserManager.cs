using System.Collections.Concurrent;
using Sphynx.Server.Utils;

namespace Sphynx.Server.User
{
    public static class SphynxUserManager
    {
        private static readonly ConcurrentDictionary<Guid, SphynxUserInfo> _userInfoCache;
        private static readonly ConcurrentDictionary<Guid, int> _cacheRetrievals;
        private const int CACHE_UPDATE_THRESHOLD = 8;

        public static event Action<SphynxUserInfo>? UserCreated;
        public static event Action<SphynxUserInfo>? UserDeleted;

        static SphynxUserManager()
        {
            _userInfoCache = new ConcurrentDictionary<Guid, SphynxUserInfo>();
            _cacheRetrievals = new ConcurrentDictionary<Guid, int>();
        }
        
        public static async Task<SphynxErrorInfo<SphynxUserInfo>> CreateUserAsync(SphynxUserCredentials credentials)
        {
            // TODO: Update database
            throw new NotImplementedException();
        }
        
        public static async Task<SphynxUserInfo?> GetUserInfoAsync(Guid userId)
        {
            if (++_cacheRetrievals[userId] > CACHE_UPDATE_THRESHOLD)
            {
                _cacheRetrievals[userId] = 0;
                // TODO: Query database
                throw new NotImplementedException();
            }

            return _userInfoCache.GetValueOrDefault(userId);
        }
    }
}