using System.Diagnostics;
using System.Net;
using System.Text;
using Sphynx.Client.Core;
using Sphynx.Client.State;
using Sphynx.Client.Utils;

namespace Sphynx.Client
{
    /// <summary>
    /// Represents the primary client interface for the application
    /// </summary>
    public sealed class SphynxClient : IDisposable
    {
        /// <summary>
        /// The version of the client
        /// </summary>
        public static readonly Version Version = new(1, 0, 0);

        /// <summary>
        /// The current state of the program
        /// </summary>
        public ISphynxState? State { get; private set; }

        /// <summary>
        /// Holds the server the client is connected to
        /// </summary>
        public SphynxServer? Server { get; private set; }

        /// <summary>
        /// Holds header string
        /// </summary>
        private static StringBuilder? _headerPool;

        /// <summary>
        /// Command-line arguments string array
        /// </summary>
        private readonly string[]? _args;

        /// <summary>
        /// Command-line arguments hash map
        /// </summary>
        public Dictionary<string, object> Arguments { get; private set; }

        /// <summary>
        /// Prefix for host argument
        /// </summary>
        internal const string HOST_ARG = "--host";

        /// <summary>
        /// Prefix for help argument
        /// </summary>
        internal static readonly HashSet<string> HELP_ARGS = new(new[] { "-h", "--help" });

        /// <summary>
        /// Prefix for version argument
        /// </summary>
        internal static readonly HashSet<string> VERSION_ARGS = new(new[] { "-v", "--version" });

        /// <summary>
        /// Prefix for user argument
        /// </summary>
        internal static readonly HashSet<string> USER_ARGS = new(new[] { "-u", "--user" });

        /// <summary>
        /// Prefix for password argument
        /// </summary>
        internal static readonly HashSet<string> PASSWORD_ARGS = new(new[] { "-p", "--password" });

        /// <summary>
        /// Constructs a <see cref="SphynxClient"/> instance
        /// </summary>
        /// <param name="args">The command-line arguments for the client. Defaults to none (null).</param>
        public SphynxClient(string[]? args = null) { _args = args; }

        /// <summary>
        /// Starts the client
        /// </summary>
        public void Start()
        {
            if (Init())
            {
                Run();
            }
            Dispose();
        }

        /// <summary>
        /// Calls cleanup on the program
        /// </summary>
        public void Dispose()
        {
            State = null;

            if (ConsoleUtils.IsVirtualTerminalEnabled())
            {
                // Switch off of alternate screen at program shutdown
                ConsoleUtils.SwitchToMain();

                // Restore console mode back to original state as necessary
                ConsoleUtils.DisableVirtualTerminal();
            }
        }

        /// <summary>
        /// Runs the application loop. Do not call this function directly; it does not properly initialize the client to be ready for execution.
        /// Instead, use <see cref="Start"/> to cause execution of this function to occur on a separate thread after having completed initialization.
        /// </summary>
        private void Run()
        {
            while (State is not null)
            {
                State.OnEnter();
                var newState = State.Run();
                State.OnExit();
                State = newState;
            }
        }

        /// <summary>
        /// Initializes the client before startup
        /// </summary>
        /// <returns><c>True</c> if execution of the rest of the application should continue after calling this function. <c>False</c> otherwise.</returns>
        private bool Init()
        {
            // Ensure proper input/output encoding capability
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            // Parse command-line arguments
            ParseArgs();
            if (!HandleArgs()) return false;

            // Enable virtual terminal sequences
            if (!ConsoleUtils.EnableVirtualTerminal())
            {
                Console.Error.WriteLine("Sphynx.Client.exe: error: client can only be run within a VT100-compatible TTY");
                return false;
            }

            // Switch to alternate screen buffer only when command-line arguments do not cause a quick exit
            ConsoleUtils.SwitchToAlternate();

            // Change default cursor type
            ConsoleUtils.SetCursorType(ConsoleUtils.CursorType.BLINKING_BAR);

            // Change console title to project title
            ConsoleUtils.SetTitle("Sphynx");

            // Connect to server
            // TODO Maybe have a list of servers to which we can connect, and attempt to connect to the first working match
            // In order for this to work, a sort of "ping" packet type may have to be implemented to test server connectivity
            Server = new SphynxServer(IPAddress.Parse((string)Arguments[HOST_ARG]));

            // Set current state to login
            State = new SphynxLoginState(this);

            // Indicate the app should continue to run as intended
            return true;
        }

        /// <summary>
        /// Parses command-line arguments found in <see cref="_args"/>
        /// </summary>
        private void ParseArgs()
        {
            Arguments = new Dictionary<string, object>();
            if (_args != null)
            {
                for (int i = 0; i < _args.Length; i++)
                {
                    string arg = _args[i].ToLower().Trim();
                    string? value = null;

                    int equalsIndex = arg.IndexOf('=');
                    if (equalsIndex != -1)
                    {
                        arg = arg[..equalsIndex];
                        value = _args[i][(equalsIndex + 1)..].TrimEnd();
                    }
                    else
                    {
                        if (i < _args.Length - 1)
                            value = _args[++i].Trim();
                    }

                    if (arg == HOST_ARG)
                    {
                        if (value is not null)
                            Arguments[HOST_ARG] = value;
                    }
                    else if (USER_ARGS.Contains(arg))
                    {
                        if (value is not null)
                            Arguments[USER_ARGS.First()] = value;
                    }
                    else if (PASSWORD_ARGS.Contains(arg))
                    {
                        if (value is not null)
                            Arguments[PASSWORD_ARGS.First()] = value;
                    }
                    else if (HELP_ARGS.Contains(arg))
                    {
                        Arguments[HELP_ARGS.First()] = value?.ToLower() != "false";
                        if (equalsIndex == -1 && value is not null && value.ToLower() != "true" && value.ToLower() != "false") i--;
                    }
                    else if (VERSION_ARGS.Contains(arg))
                    {
                        Arguments[VERSION_ARGS.First()] = value?.ToLower() != "false";
                        if (equalsIndex == -1 && value is not null && value.ToLower() != "true" && value.ToLower() != "false") i--;
                    }
                }
            }

            // TODO Make list of defaults host(s)/server(s) to which the client should connect
            Arguments.TryAdd(HOST_ARG, "127.0.0.1");
        }

        /// <summary>
        /// Handles command-line arguments by executes their appropriate tasks.
        /// </summary>
        /// <returns><c>True</c> if execution of the rest of the application should continue after calling this function. <c>False</c> otherwise.</returns>
        private bool HandleArgs()
        {
            if (Arguments.TryGetValue(HELP_ARGS.First(), out var printHelp) && (bool)printHelp)
            {
                const string helpMessage = "usage: Sphynx.Client.exe [options]...\n" +
                                           "options:\n" +
                                           "  -v, --version         display program version information.\n" +
                                           "  -h, --help            display this information.\n" +
                                           "  -u, --user <user>     set user login information.\n" +
                                           "  -p, --password <pass> set user password information.\n" +
                                           "      --host <host>     set a specific server host to connect to.\n" +
                                           "\n" +
                                           "For bug reporting instructions, please see:\n" +
                                           "<https://github.com/Ark1409/Sphynx>";

                Console.WriteLine(helpMessage);

                // Mark that the app should not be run, exit immediately instead.
                return false;
            }

            if (Arguments.TryGetValue(VERSION_ARGS.First(), out var printVersion) && (bool)printVersion)
            {
                string versionMessage = $"Sphynx.Client.exe (Sphynx.Client) version {Version}-dev\n" +
                                        $"Copyright (C) {DateTime.Now.Year} Sphynx Project\n" +
                                        "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\n" +
                                        "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\n" +
                                        "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.\n" +
                                        "\n" +
                                        "For bug reporting instructions, please see:\n" +
                                        "<https://github.com/Ark1409/Sphynx>";

                Console.WriteLine(versionMessage);

                // Mark that the app should not be run, exit immediately instead.
                return false;
            }

            if (Arguments.TryGetValue(HOST_ARG, out var host))
            {
                if (!IPAddress.TryParse(host.ToString(), out _))
                {
                    // Indicate an error in parsing the host string if the provided host was not in a valid format
                    Console.Error.WriteLine($"Sphynx.Client.exe: error: invalid host '{host}'");
                    return false;
                }
            }

            // The app should continue to run normally otherwise
            return true;
        }

        /// <summary>
        /// Retrieves global header for the program
        /// </summary>
        /// <returns>The global program header, as a string</returns>
        public static string GetHeader()
        {
            if (_headerPool != null) return _headerPool.ToString();

            _headerPool = new StringBuilder();

            const string HEADER_PROGRAM_NAME = "SPHYNX";

            int headerWidth = Math.Max(Console.BufferWidth, HEADER_PROGRAM_NAME.Length); // Width msut be at least length of program text
            int headerHeight = Math.Max(6, 3); // Height must be at least three

            // Print first line of hashes
            for (int i = 0; i < headerWidth; i++) { _headerPool.Append('#'); }
            _headerPool.Append('\n');

            // Now print "#     #" lines before the program name
            for (int line = 0; line < (headerHeight - 2) / 2; line++)
            {
                for (int i = 0; i < headerWidth; i++)
                {
                    _headerPool.Append(i == 0 || i == headerWidth - 1 ? '#' : ' ');
                }
            }

            // Print the program name
            {
                for (int i = 0; i < (headerWidth - HEADER_PROGRAM_NAME.Length) / 2; i++)
                {
                    _headerPool.Append(i == 0 ? '#' : ' ');
                }

                _headerPool.Append(HEADER_PROGRAM_NAME);

                for (int i = (headerWidth + HEADER_PROGRAM_NAME.Length) / 2; i < headerWidth; i++)
                {
                    _headerPool.Append(i == headerWidth - 1 ? '#' : ' ');
                }
                _headerPool.Append('\n');
            }

            // Now print "#     #" lines after the program name
            for (int line = 0; line < (headerHeight - 2) / 2; line++)
            {
                for (int i = 0; i < headerWidth; i++)
                {
                    _headerPool.Append(i == 0 || i == headerWidth - 1 ? '#' : ' ');
                }
            }

            // Print last line of hashes
            for (int i = 0; i < headerWidth; i++) { _headerPool.Append('#'); }
            _headerPool.Append("\n");

            return _headerPool.ToString();
        }

        public static int Run(string[]? args)
        {
            SphynxClient? client = null;
            try
            {
                client = new SphynxClient(args);
                client.Start();
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                Debug.Fail(e.ToString());
                Thread.Sleep(500);
                client?.Dispose();
                return 1;
            }
        }
    }
}
