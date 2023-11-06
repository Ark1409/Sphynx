using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Sphynx.Client.Core;
using Sphynx.Client.State;

namespace Sphynx.Client
{
    public class SphynxClient : IDisposable
    {
        /// <summary>
        /// The current state of the program
        /// </summary>
        public ISphynxState? State { get; private set; }

        /// <summary>
        /// Holds the server the client is connected to
        /// </summary>
        public SphynxServer Server { get; private set; }

        /// <summary>
        /// Holds header string
        /// </summary>
        private static StringBuilder? _headerPool;

        /// <summary>
        /// Starts the client
        /// </summary>
        public void Start()
        {
            Init();

            var t = new Thread(Run);
            t.Start();
            t.Join();

            Dispose();
        }

        /// <summary>
        /// Intializes the client before startup
        /// </summary>
        private void Init()
        {
            // Connect to server
            Server = new SphynxServer(IPAddress.Parse("127.0.0.1"));

            // Set current state to login
            State = new SphynxLoginState(this);
        }

        /// <summary>
        /// Calls cleanup on the program
        /// </summary>
        public void Dispose()
        {
            State = null;
        }

        /// <summary>
        /// Runs the application loop
        /// </summary>
        private void Run()
        {
            while (State != null)
            {
                State = State.Run();
            }
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
