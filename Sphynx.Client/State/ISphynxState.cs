using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Sphynx.Client.State
{
    public interface ISphynxState
    {
        /// <summary>
        /// Starts execution of the current state
        /// </summary>
        /// <returns>The next state to be ran, or null if the program should (immediately) terminate</returns>
        ISphynxState? Run();
    }
}
