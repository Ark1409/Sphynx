using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sphynx.Client.Core;
using Sphynx.Client.State;

namespace Sphynx.Client
{
    public sealed class SphynxClient : IDisposable
    {
        /// <summary>
        /// The version of the client
        /// </summary>
        public static readonly Version Version = new Version(1, 0, 0);

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
        public Dictionary<string, object> Arguments { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Prefix for host argument
        /// </summary>
        internal static string HOST_ARG = "--host";

        /// <summary>
        /// Prefix for help argument
        /// </summary>
        internal static readonly string[] HELP_ARGS = { "--help", "-h" };

        /// <summary>
        /// Prefix for version argument
        /// </summary>
        internal static readonly string[] VERSION_ARGS = { "--version", "-v" };

        /// <summary>
        /// Prefix for user argument
        /// </summary>
        internal static readonly string[] USER_ARGS = { "--user", "-u" };

        // <summary>
        /// Prefix for password argument
        /// </summary>
        internal static readonly string[] PASSWORD_ARGS = { "--password", "-p" };

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
            ParseArgs();
            if (!HandleArgs()) return;

            Init();

            var t = new Thread(Run);
            t.Start();
            t.Join();

            Dispose();
        }

        /// <summary>
        /// Calls cleanup on the program
        /// </summary>
        public void Dispose()
        {
            State = null;
        }

        /// <summary>
        /// Runs the application loop. Do not call this function directly; it does not properly intialize the client to be ready for execution.
        /// Instead, use <see cref="Start"/> to cause execution of this function to occur on a separate thread after having completed intialization.
        /// </summary>
        private void Run()
        {
            while (State != null)
            {
                State = State.Run();
            }
        }

        /// <summary>
        /// Intializes the client before startup
        /// </summary>
        private void Init()
        {
            // Connect to server
            Server = new SphynxServer(IPAddress.Parse((string)Arguments[HOST_ARG]));

            // Set current state to login
            State = new SphynxLoginState(this);
        }

        /// <summary>
        /// Parses command-line arguments found in <see cref="_args"/>
        /// </summary>
        private void ParseArgs()
        {
            Arguments = new Dictionary<string, object>();

            if (_args != null)
            {
                for (uint i = 0; i < _args.Length; i++)
                {
                    string arg = _args[i].ToLower();
                    if (arg.StartsWith(HOST_ARG))
                    {
                        if (arg.Length > HOST_ARG.Length && arg[HOST_ARG.Length] == '=')
                        {
                            if (Arguments.ContainsKey(HOST_ARG))
                            {
                                // TODO warn/error
                            }
                            Arguments[HOST_ARG] = arg.Substring(HOST_ARG.Length + "=".Length);
                        }
                        else if (arg.Length == HOST_ARG.Length)
                        {
                            if (i < _args.Length - 1)
                            {
                                if (Arguments.ContainsKey(HOST_ARG))
                                {
                                    // TODO warn/error
                                }
                                Arguments[HOST_ARG] = _args[++i];
                            }
                        }
                    }

                    foreach (string userCmd in USER_ARGS)
                    {
                        if (arg.StartsWith(userCmd))
                        {
                            if (arg.Length > userCmd.Length && arg[userCmd.Length] == '=')
                            {
                                if (Arguments.ContainsKey(USER_ARGS[0]))
                                {
                                    // TODO warn/error
                                }
                                Arguments[USER_ARGS[0]] = arg.Substring(userCmd.Length + "=".Length);
                                break;
                            }
                            else if (arg.Length == userCmd.Length)
                            {
                                if (i < _args.Length - 1)
                                {
                                    if (Arguments.ContainsKey(USER_ARGS[0]))
                                    {
                                        // TODO warn/error
                                    }
                                    Arguments[USER_ARGS[0]] = _args[++i];
                                    break;
                                }
                            }
                        }
                    }

                    foreach (string passCmd in PASSWORD_ARGS)
                    {
                        if (arg.StartsWith(passCmd))
                        {
                            if (arg.Length > passCmd.Length && arg[passCmd.Length] == '=')
                            {
                                if (Arguments.ContainsKey(PASSWORD_ARGS[0]))
                                {
                                    // TODO warn/error
                                }
                                Arguments[PASSWORD_ARGS[0]] = arg.Substring(passCmd.Length + "=".Length);
                                break;
                            }
                            else if (arg.Length == passCmd.Length)
                            {
                                if (i < _args.Length - 1)
                                {
                                    if (Arguments.ContainsKey(PASSWORD_ARGS[0]))
                                    {
                                        // TODO warn/error
                                    }
                                    Arguments[PASSWORD_ARGS[0]] = _args[++i];
                                    break;
                                }
                            }
                        }
                    }

                    foreach (string helpCmd in HELP_ARGS)
                    {
                        if (arg == helpCmd)
                        {
                            Arguments[HELP_ARGS[0]] = true;
                            break;
                        }
                    }

                    foreach (string versionCmd in VERSION_ARGS)
                    {
                        if (arg == versionCmd)
                        {
                            Arguments[VERSION_ARGS[0]] = true;
                            break;
                        }
                    }
                }
            }

            Arguments.TryAdd(HOST_ARG, "127.0.0.1");
        }

        /// <summary>
        /// Handles command-line arguments by executes their appropriate tasks.
        /// </summary>
        /// <returns><c>True</c> if execution of the rest of the application should continue after calling this function. <c>False</c> otherwise.</returns>
        private bool HandleArgs()
        {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name!;

            if (Arguments.TryGetValue(HELP_ARGS[0], out var printHelp) && (bool)printHelp)
            {
                string helpMessage =       $"usage: {assemblyName}.exe [options]...\n" +
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

            if (Arguments.TryGetValue(VERSION_ARGS[0], out var printVersion) && (bool)printVersion)
            {
                string versionMessage = $"{assemblyName}.exe (Sphynx.Client) version {Version}-dev~features/states\n" +
                                        $"Copyright (C) 2023 Ark -A-\n" +
                                        $"THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\n" +
                                        $"IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\n" +
                                        $"FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.\n" +
                                        $"\n" +
                                        $"For bug reporting instructions, please see:\n" +
                                        $"<https://github.com/Ark1409/Sphynx>";

                Console.WriteLine(versionMessage);
                return false;
            }

            if (Arguments.TryGetValue(HOST_ARG, out var host))
            {
                if (!IPAddress.TryParse(host.ToString(), out var _))
                {
                    Console.WriteLine($"{assemblyName}.exe: error: invalid host '{host}'");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Retreives global header for the program
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
    }
}
