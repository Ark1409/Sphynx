// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Model.User;
using Sphynx.Test.Utils;

namespace Sphynx.Test.Model.User
{
    public class TestSphynxSelfInfo : SphynxSelfInfo
    {
        public TestSphynxSelfInfo(string name = "test-self")
        {
            UserName = name;
            UserId = name.AsGuid();
            CreatedAt = DateTimeOffset.MinValue.AddYears(name.Length);
            LastLogin = DateTimeOffset.MinValue.AddYears(name.Length*100);

            var statuses = Enum.GetValues<SphynxUserStatus>();
            UserStatus = statuses[name.Length % statuses.Length];
        }

        public static TestSphynxSelfInfo[] FromNames(params string[] names)
        {
            var users = new TestSphynxSelfInfo[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                users[i] = new TestSphynxSelfInfo(names[i]);
            }

            return users;
        }

        public override bool Equals(SphynxSelfInfo? other)
        {
            return base.Equals(other) && LastLogin == other.LastLogin;
        }
    }
}
