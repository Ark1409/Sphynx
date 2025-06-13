// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.ModelV2.User;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.User
{
    public class TestSphynxUserInfo : SphynxUserInfo
    {
        public SnowflakeId UserId { get; set; }
        public string UserName { get; set; }
        public SphynxUserStatus UserStatus { get; set; }

        public TestSphynxUserInfo(string userName = "Test")
        {
            UserName = userName;
            UserId = userName.AsSnowflakeId();

            var statuses = Enum.GetValues<SphynxUserStatus>();
            UserStatus = statuses[userName.Length % statuses.Length];
        }

        public static TestSphynxUserInfo[] FromArray(params string[] names)
        {
            var users = new TestSphynxUserInfo[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                users[i] = new TestSphynxUserInfo(names[i]);
            }

            return users;
        }

        public bool Equals(SphynxUserInfo? other)
        {
            return UserId.Equals(other?.UserId) && UserName == other?.UserName && UserStatus == other.UserStatus;
        }
    }
}
