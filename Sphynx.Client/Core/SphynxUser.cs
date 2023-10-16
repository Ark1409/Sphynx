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
            OFFLINE,
            ONLINE,
            AWAY,
            DND
        }

        public UserStatus Status { get; set; } = UserStatus.OFFLINE;

        public string Name { get; protected set; }

        public string Password { get; set; }

        // TODO Implement with server
        public static bool IsValid(string username, string password) => true;

        public static SphynxUser? Login(string username, string password) => IsValid(username, password) ?
            new SphynxUser() { Status = UserStatus.OFFLINE, Name = username, Password = password } : null;


    }
}
