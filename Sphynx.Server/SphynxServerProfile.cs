// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Sphynx.Server
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
        public virtual IPEndPoint EndPoint { get; set; }

        /// <summary>
        /// The primary logger factory which will be used by the server.
        /// </summary>
        public virtual ILoggerFactory LoggerFactory { get; set; }

        /// <summary>
        /// Retrieves the default server logging instance.
        /// </summary>
        public virtual ILogger Logger => _logger ??= LoggerFactory.CreateLogger(typeof(SphynxServer));

        private ILogger _logger;

        /// <summary>
        /// Whether this profile has been disposed. The profile should no longer be used to configure a <see cref="SphynxServer"/> once disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        protected SphynxServerProfile(bool configure = false)
        {
            if (configure)
                ConfigureServices();
        }

        private void ConfigureServices()
        {
            EndPoint = DefaultEndPoint;
            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "[MM-dd-yyyy HH:mm:ss] ";
                });
            });
        }

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
