// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Sphynx.ServerV2
{
    /// <summary>
    /// Represents a collection of all the services used by a <see cref="SphynxServer"/> throughout its execution.
    /// </summary>
    /// <remarks>The profile should be configured and <see cref="SphynxServer(SphynxServerProfile)">passed to a server</see>
    /// during the bootstrap process.</remarks>
    public abstract class SphynxServerProfile : IDisposable
    {
        /// <summary>
        /// The default IP endpoint for a <see cref="SphynxServer"/>.
        /// </summary>
        public static readonly IPEndPoint DefaultEndPoint = new(IPAddress.Any, DEFAULT_PORT);

        /// <summary>
        /// The default port for socket information exchange between client and server.
        /// </summary>
        public const short DEFAULT_PORT = 2000;

        /// <summary>
        /// Returns the endpoint to be associated with the server.
        /// </summary>
        public IPEndPoint EndPoint { get; init; } = DefaultEndPoint;

        /// <summary>
        /// The primary logger factory which will be used by the server.
        /// </summary>
        public virtual ILoggerFactory LoggerFactory { get; init; } = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options =>
            {
                options.FormatterName = ConsoleFormatterNames.Simple;
                options.QueueFullMode = ConsoleLoggerQueueFullMode.DropWrite;
            });
        });

        /// <summary>
        /// Retrieves the default server logging instance.
        /// </summary>
        public virtual ILogger Logger => _logger ??= LoggerFactory.CreateLogger(typeof(SphynxServer));

        private ILogger? _logger;

        /// <summary>
        /// Whether this profile has been disposed. The profile should no longer be used to configure a <see cref="SphynxServer"/> once disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Disposes of this server profile.
        /// The profile should no longer be used to configure a <see cref="SphynxServer"/> once disposed.
        /// </summary>
        /// <param name="disposing">Whether we're entering from the dispose method.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            if (disposing)
            {
                LoggerFactory?.Dispose();
            }
        }

        /// <summary>
        /// Disposes of this server profile.
        /// The profile should no longer be used to configure a <see cref="SphynxServer"/> once disposed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
