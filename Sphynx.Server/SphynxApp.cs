using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sphynx.Server.State;

namespace Sphynx.Server
{
    /// <summary>
    /// A class which represents the local app instance of the Sphynx Server.
    /// </summary>
    public static class SphynxApp
    {
        /// <summary>
        /// The underlying server through which users connect.
        /// </summary>
        public static SphynxServer? Server { get; set; }

        /// <summary>
        /// The program arguments.
        /// </summary>
        public static string[]? Arguments { get; set; }

        /// <summary>
        /// A collection of all the states of the application instance.
        /// </summary>
        public static StateCollection? StateCollection { get; set; }

        /// <summary>
        /// The current state of the application instance.
        /// </summary>
        public static ISphynxState? CurrentState { get; set; }

        /// <summary>
        /// Whether this application instance is currently running. The application instance is independent from the server.
        /// </summary>
        public static bool Running { get; private set; }

        /// <summary>
        /// Runs the app instance along with the server.
        /// </summary>
        /// <param name="args">The program arguments.</param>
        public static void Run(string[] args)
        {
            StateCollection = new StateCollection();
            CurrentState = StateCollection.GetState<MenuState>();
            Arguments = args;

            Server = new SphynxServer();
            Server.Start();
            Running = true;

            while (CurrentState != null)
            {
                CurrentState = CurrentState.Run();
            }

            Server.Dispose();
        }
    }
}
