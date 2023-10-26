using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Client.Core
{
    public class SphynxUser
    {
        public enum UserStatus : byte
        {
            OFFLINE = 0,
            ONLINE,
            AWAY,
            DND
        }

        public UserStatus Status { get; set; } = UserStatus.OFFLINE;

        public string Name { get; private set; } = "";

        public string? Password { get; private set; }

        public static SphynxUser? GetUser(string username)
        {
            // TODO implement with server
            return new SphynxUser() { Name = username };
        }

        public static bool ValidateUser(string username, string password)
        {
            // TODO implement with server
            return true;
        }
    }
}
