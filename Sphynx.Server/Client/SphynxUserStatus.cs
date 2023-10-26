using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.Client
{
    /// <summary>
    /// Represents that status of a user.
    /// </summary>
    public enum SphynxUserStatus : byte
    {
        OFFLINE = 0,
        ONLINE,
        AWAY,
        DND
    }
}
