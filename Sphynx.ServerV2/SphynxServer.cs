// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;

namespace Sphynx.ServerV2
{
    /// <summary>
    /// Represents a generic server instance which accepts clients on a specific endpoint.
    /// </summary>
    public abstract class SphynxServer : IAsyncDisposable
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
        /// The start task representing the running state of the server.
        /// </summary>
        protected Task? ServerTask => _serverTask;

        private volatile Task? _serverTask;

        private CancellationTokenSource _serverCts = new();
        private readonly AsyncLocal<bool> _isInsideServerTask = new();
        private readonly SemaphoreSlim _startSemaphore = new(1, 1);

        private volatile bool _disposed;

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
        /// <returns>The started server task.</returns>
        /// <exception cref="ObjectDisposedException">If this server has already been disposed.</exception>
        /// <exception cref="OperationCanceledException">If this server has already been stopped.</exception>
        public async ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfStopped();

            await _startSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Propagate exceptions to concurrent callers
            if (_serverTask is not null)
            {
                await _serverTask.ConfigureAwait(false);
                return;
            }

            if (_serverCts.IsCancellationRequested)
                return;

            OnStart?.Invoke(this);

            try
            {
                _serverCts = CancellationTokenSource.CreateLinkedTokenSource(_serverCts.Token, cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Server has been disposed of. The CTS should be cancelled.
            }

            Profile.Logger.LogDebug("Starting {ServerName}...", Name);

            try
            {
                if (!_serverCts.IsCancellationRequested)
                {
                    try
                    {
                        _isInsideServerTask.Value = true;
                        await (_serverTask = OnStartAsync(_serverCts.Token)).ConfigureAwait(false);
                    }
                    finally
                    {
                        _isInsideServerTask.Value = false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Likely server stopped
            }
            catch (Exception ex)
            {
                Profile.Logger.LogCritical(ex, "An unhandled exception occured during server execution");
            }

            _startSemaphore.Release();

            Profile.Logger.LogDebug("Stopping {ServerName}...", Name);
            await StopAsync().ConfigureAwait(false);
        }

        private void ThrowIfStopped()
        {
            ThrowIfDisposed();

            if (_serverCts.IsCancellationRequested)
                throw new OperationCanceledException("The operation was canceled.");
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Called once the server has been instructed to start and should begin running.
        /// This method is typically where the server's read loop is initiated.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to control the running state of the server. Once cancelled,
        /// it is expected that the server should begin its shutdown process.</param>
        /// <returns>The server's running task. The task should not complete until the server has begun its shutdown process.</returns>
        protected abstract Task OnStartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the server, optionally waiting for its termination.
        /// </summary>
        /// <param name="waitForFinish">Whether to wait the server to finish.</param>
        /// <returns>A task representing the stop operation. If <paramref name="waitForFinish"/> is true, this task will not
        /// complete until the server has terminated; else, it will return after sending a stop signal.</returns>
        /// <remarks>This method does not dispose of the server's resources. <see cref="DisposeAsync"/> should be called for
        /// that.</remarks>
        public ValueTask StopAsync(bool waitForFinish = true)
        {
            // We allow the server to be stopped even when disposed. Just makes our lives easier.
            if (_disposed)
                return ValueTask.CompletedTask;

            // Signal for stop
            if (!_serverCts.IsCancellationRequested)
            {
                try
                {
                    _serverCts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Since we are not acquiring the semaphore before cancelling, it's technically
                    // possible for a concurrent disposal to sneak in after the previous
                    // cancellation check. This can technically be guarded against by yet another
                    // semaphore, but that would potentially make this unlikely path non-synchronous,
                    // which might confuse the caller when waitForFinish == false.
                }
            }

            if (_isInsideServerTask.Value || !waitForFinish)
                return ValueTask.CompletedTask;

            return WaitAsync();
        }

        /// <summary>
        /// Waits for the server to finish execution.
        /// </summary>
        private async ValueTask WaitAsync()
        {
            if (_disposed)
                return;

            if (_serverTask?.IsCompleted ?? false)
                return;

            await _startSemaphore.WaitAsync().ConfigureAwait(false);
            _startSemaphore.Release();
        }

        /// <summary>
        /// Returns the server's <see cref="Name"/>.
        /// </summary>
        /// <returns>The server's name.</returns>
        public override string ToString() => Name;

        private async ValueTask DisposeServerAsync()
        {
            await _startSemaphore.WaitAsync().ConfigureAwait(false);

            if (_disposed)
                return;

            try
            {
                _serverCts.Dispose();
                Profile.Dispose();
            }
            catch
            {
                // We don't really care at this point
            }

            _disposed = true;
            _startSemaphore.Release();
        }

        /// <summary>
        /// Asynchronously disposes of all resources held by this <see cref="SphynxServer"/>.
        /// </summary>
        public virtual async ValueTask DisposeAsync()
        {
            // Concurrent disposal should be fine
            if (_disposed)
                return;

            OnStart = null;

            await StopAsync().ConfigureAwait(false);
            await DisposeServerAsync().ConfigureAwait(false);
        }
    }
}
