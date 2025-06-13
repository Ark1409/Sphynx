using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphynx.Server.AppState
{
    /// <summary>
    /// Represents an applicaiton state.
    /// </summary>
    public interface ISphynxState
    {
        /// <summary>
        /// Runs the current application state, blocking until it is finished; and returns the next state, or null if there are no
        /// more states to run.
        /// </summary>
        /// <returns>The next state to run, or null if there are no more.</returns>
        ISphynxState? Run();
    }
}
