using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sphynx.Server.AppState;

namespace Sphynx.Server
{
    /// <summary>
    /// A class which represents the local app instance of the Sphynx Server.
    /// </summary>
    public static class SphynxApp
    {
        /// <summary>
        /// Gets the first registered <see cref="SphynxServer"/>.
        /// </summary>
        public static SphynxServer? Server { private set; get; }

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
        public static bool Running => Volatile.Read(ref _running) == 1;
        private static int _running;

        /// <summary>
        /// Runs the app instance along with the server.
        /// </summary>
        /// <param name="args">The program arguments.</param>
        public static void Run(string[] args)
        {
            if (Interlocked.Exchange(ref _running, 1) != 0)
                return;
            
            StateCollection = new StateCollection();
            CurrentState = StateCollection.GetState<MenuState>();
            Arguments = args;

            Server = new SphynxServer();
            Server.Start();

            while (CurrentState != null)
            {
                CurrentState = CurrentState.Run();
            }

            AppDomain.CurrentDomain.ProcessExit += (_, e) => Server.Dispose();
            AppDomain.CurrentDomain.UnhandledException += (_, e) => Server.Dispose();

            Server.Dispose();
        }
    }
}