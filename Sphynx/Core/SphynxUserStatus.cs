using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Core
{
    /// <summary>
    /// Possible statuses for a <see cref="SphynxSessionUser"/>.
    /// </summary>
    public enum SphynxUserStatus : byte
    {
        OFFLINE = 0,
        ONLINE,
        AWAY,
        DND
    }
}
