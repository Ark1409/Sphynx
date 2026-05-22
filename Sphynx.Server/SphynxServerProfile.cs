// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Net;
using Microsoft;
using Microsoft.Extensions.Logging;

namespace Sphynx.Server
{
    /// <summary>
    /// Represents a collection of all the services used by a <see cref="SphynxServer"/> throughout its execution.
    /// </summary>
    /// <remarks>The profile should be configured and <see cref="SphynxServer(SphynxServerProfile)">passed to a server</see>
    /// during the bootstrap process.</remarks>
    public abstract class SphynxServerProfile : IDisposableObservable
    {
        /// <summary>
        /// The default IP endpoint for a <see cref="SphynxServer"/>.
        /// </summary>
        private static readonly IPEndPoint _defaultEndPoint = new(IPAddress.Any, DEFAULT_PORT);

        /// <summary>
        /// The default port for socket information exchange between client and server.
        /// </summary>
        public const short DEFAULT_PORT = 0x5350; // SP

        /// <summary>
        /// Returns the endpoint to be associated with the server.
        /// </summary>
        public IPEndPoint EndPoint { get; set; }

        /// <summary>
        /// The primary logger factory which will be used by the server.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; set; }

        /// <summary>
        /// Retrieves the default server logging instance.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Whether this profile has been disposed. The profile should no longer be used to configure a <see cref="SphynxServer"/> once disposed.
        /// </summary>
        public virtual bool IsDisposed { get; set; }

        private readonly object _syncLock = new();
        private bool _configured;

        protected SphynxServerProfile()
        {
        }

        public virtual bool ConfigureProfile()
        {
            lock (_syncLock)
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);

                if (_configured)
                    return false;

                EndPoint ??= GetDefaultEndPoint();
                LoggerFactory ??= GetDefaultLoggerFactory();
                Logger ??= GetDefaultLogger(LoggerFactory);
                return _configured = true;
            }
        }

        public static ILogger GetDefaultLogger(ILoggerFactory factory) => factory.CreateLogger(typeof(SphynxServer));

        /// <inheritdoc cref="_defaultEndPoint"/>
        public static IPEndPoint GetDefaultEndPoint() => _defaultEndPoint;

        public static ILoggerFactory GetDefaultLoggerFactory() => Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "[MM-dd-yyyy HH:mm:ss] ";
            });
        });

        /// <summary>
        /// Disposes of this server profile.
        /// The profile should no longer be used to configure a <see cref="SphynxServer"/> once disposed.
        /// </summary>
        /// <param name="disposing">Whether we're entering from the dispose method.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            lock (_syncLock)
            {
                if (IsDisposed)
                    return;

                if (disposing)
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
