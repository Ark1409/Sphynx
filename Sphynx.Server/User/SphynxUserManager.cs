using System.Collections.Concurrent;
using Sphynx.Server.Utils;

namespace Sphynx.Server.User
{
    public static class SphynxUserManager
    {
        private static readonly ConcurrentDictionary<Guid, SphynxUserInfo> _userInfoCache;
        private static int _cacheRetrivals;
        private const int CACHE_UPDATE_THRESHOLD = 8;

        public static event Action<SphynxUserInfo>? UserCreated;
        public static event Action<SphynxUserInfo>? UserDeleted;

        static SphynxUserManager()
        {
            _userInfoCache = new ConcurrentDictionary<Guid, SphynxUserInfo>();
        }
        
        public static async Task<SphynxErrorInfo<SphynxUserInfo>> CreateUserAsync(SphynxUserCredentials credentials)
        {
            // TODO: Update database
            throw new NotImplementedException();
        }
        
        public static async Task<SphynxUserInfo?> GetUserInfoAsync(Guid userId)
        {
            if (++_cacheRetrivals > CACHE_UPDATE_THRESHOLD)
            {
                // TODO: Query database
                throw new NotImplementedException();
            }

            return _userInfoCache.GetValueOrDefault(userId);
        }
    }
}