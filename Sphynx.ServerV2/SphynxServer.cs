// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;

namespace Sphynx.ServerV2
{
    /// <summary>
    /// Represents a generic server instance which accepts clients on a specific endpoint.
    /// </summary>
    public abstract class SphynxServer : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Retrieves the running state of the server.
        /// </summary>
        public bool IsRunning => !_serverTask?.IsCompleted ?? false;

        /// <summary>
        /// The name of this <see cref="SphynxServer"/>.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// An event which fired before the server starts. This can be used as a last attempt to inject
        /// some configurations into the server.
        /// </summary>
        public event Action<SphynxServer>? OnStart;

        /// <summary>
        /// The profile with which to configure the server.
        /// </summary>
        public virtual SphynxServerProfile Profile { get; }

        /// <summary>
        /// Cancellation token source for the server running state.
        /// </summary>
        protected CancellationTokenSource ServerCts { get; private set; } = new();

        /// <summary>
        /// The start task representing the running state of the server.
        /// </summary>
        protected Task? ServerTask => _serverTask;

        private volatile Task? _serverTask;
        private readonly AsyncLocal<bool> _isInsideServerTask = new();

        private readonly SemaphoreSlim _startStopSemaphore = new(1, 1);

        // 0 = not started; 1 = started
        private int _started;

        // 0 = not disposed; 1 = disposing/disposed
        private int _disposed;

        /// <summary>
        /// Creates (but does not start) a new <see cref="SphynxServer"/> using the specified <paramref name="profile"/>.
        /// </summary>
        /// <param name="profile">The profile with which the server should be configured.</param>
        public SphynxServer(SphynxServerProfile profile) : this(profile, null)
        {
        }

        /// <summary>
        /// Creates (but does not start) a new <see cref="SphynxServer"/> with the given <paramref name="name"/>
        /// using the specified <paramref name="profile"/>.
        /// </summary>
        /// <param name="profile">The profile with which the server should be configured.</param>
        /// <param name="name">A user-friendly name for the server.</param>
        public SphynxServer(SphynxServerProfile profile, string? name)
        {
            ArgumentNullException.ThrowIfNull(profile, nameof(profile));

            if (profile.IsDisposed)
                throw new ArgumentException("Cannot use a disposed profile to configure a server", nameof(profile));

            Profile = profile;
            Name = name ?? $"{GetType().Name}@{profile.EndPoint}";
        }

        /// <summary>
        /// Starts the server, optionally with a cancellation token which can be used to stop the server. On completion, this function
        /// will also stop the server, and it may be stopped manually by calling <see cref="StopAsync"/>.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to control the running state of the server.</param>
        /// <returns>The started server task. If the server has already been started, this should return a completed task.</returns>
        /// <exception cref="ObjectDisposedException">If this server has already been disposed.</exception>
        /// <exception cref="OperationCanceledException">If this server has already been stopped.</exception>
        public async ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfStopped();

            if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
                return;

            ServerCts = CancellationTokenSource.CreateLinkedTokenSource(ServerCts.Token, cancellationToken);

            if (ServerCts.IsCancellationRequested)
            {
                await DisposeAsync().ConfigureAwait(false);
                return;
            }

            OnStart?.Invoke(this);

            try
            {
                Profile.Logger.LogDebug("Starting server...");

                await _startStopSemaphore.WaitAsync(ServerCts.Token).ConfigureAwait(false);

                try
                {
                    _isInsideServerTask.Value = true;
                    await (_serverTask = OnStartAsync(ServerCts.Token)).ConfigureAwait(false);
                }
                finally
                {
                    _startStopSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Server stopped
            }
            catch (Exception ex)
            {
                Profile.Logger.LogCritical(ex, "An unhandled exception occured during server execution");
            }

            _isInsideServerTask.Value = false;

            Profile.Logger.LogInformation("Stopping server...");

            await StopAsync().ConfigureAwait(false);
        }

        private void ThrowIfStopped()
        {
            ThrowIfDisposed();
            ServerCts.Token.ThrowIfCancellationRequested();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Called once the server has been instructed to start and should begin running.
        /// This method is typically where the server's read loop is initiated.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to control the running state of the server. Once cancelled,
        /// it is expected that the server should begin its shutdown process.</param>
        /// <returns>The server's running task. The task should not complete at least until the server has begun its shutdown process.</returns>
        protected abstract Task OnStartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the server, optionally waiting for its termination.
        /// </summary>
        /// <param name="waitForFinish">Whether to wait the server to finish.</param>
        /// <returns>A task representing the stop operation. If <paramref name="waitForFinish"/> is true, this task will not
        /// complete until the server has terminated; else, it will return after sending a stop signal.</returns>
        /// <remarks>This method does not dispose of the server's resources. <see cref="Dispose"/> or <see cref="DisposeAsync"/>
        /// should be called for that.</remarks>
        public ValueTask StopAsync(bool waitForFinish = true)
        {
            if (Volatile.Read(ref _disposed) != 0)
                return ValueTask.FromException(new ObjectDisposedException(GetType().FullName));

            return StopInternalAsync(waitForFinish);
        }

        private async ValueTask StopInternalAsync(bool waitForFinish)
        {
            // Fast path
            if (ServerCts.IsCancellationRequested && !waitForFinish)
                return;

            // If we are stopping from the server loop, simply return
            if (_isInsideServerTask.Value)
                return;

            ServerCts.Cancel();

            if (waitForFinish)
            {
                // It shouldn't be possible to deadlock through reentrancy here, since we already return early
                // if we came from the run loop
                await _startStopSemaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    var serverTask = _serverTask;

                    if (serverTask is not null)
                        await serverTask;
                }
                finally
                {
                    _startStopSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// Returns the server's <see cref="Name"/>.
        /// </summary>
        /// <returns>The server's name.</returns>
        public override string ToString() => Name;

        /// <summary>
        /// Disposes of all resources held by this <see cref="SphynxServer"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            if (disposing)
            {
                OnStart = null;

                var stopTask = StopInternalAsync(true);

                if (!stopTask.IsCompleted)
                    stopTask.AsTask().Wait();

                ServerCts.Dispose();
                Profile.Dispose();
            }
        }

        /// <inheritdoc/>
        public virtual ValueTask DisposeAsync()
        {
            try
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
            catch (Exception ex)
            {
                return ValueTask.FromException(ex);
            }
        }
    }
}
