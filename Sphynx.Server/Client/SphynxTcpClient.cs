// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Sphynx.Network;
using Sphynx.Network.Packet;
using Sphynx.Server.Infrastructure.Routing;
using Sphynx.Utils;

namespace Sphynx.Server.Client
{
    public class SphynxTcpClientOptions
    {
        /// <summary>
        /// The unique ID for this client.
        /// </summary>
        public Guid ClientId { get; init; }

        /// <summary>
        /// The <see cref="SphynxMessageClient"/> which is used to send and receive <see cref="SphynxMessage"/>s.
        /// </summary>
        public MessageClientFactory MessageClientFactory { get; init; }

        /// <summary>
        /// The packet router which is used to route incoming packets to handlers.
        /// </summary>
        public IMessageRouter MessageRouter { get; init; }

        /// <summary>
        /// The logger factory to use to create the client's logger.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; init; }

        /// <summary>
        /// Event that is fired when this client disconnects from the server. This may run concurrently
        /// with <see cref="SphynxTcpClient.DisposeAsync()"/>.
        /// </summary>
        public Func<SphynxTcpClient, Exception?, ValueTask>? OnDisconnect { get; init; }

        public SphynxTcpClientOptions(Guid? clientId, MessageClientFactory messageClientFactory, IMessageRouter messageRouter,
            ILoggerFactory loggerFactory, Func<SphynxTcpClient, Exception?, ValueTask>? onDisconnect)
        {
            ClientId = clientId ?? Guid.NewGuid();
            MessageClientFactory = messageClientFactory;
            MessageRouter = messageRouter;
            LoggerFactory = loggerFactory;
            OnDisconnect = onDisconnect;
        }

        public SphynxTcpClientOptions(SphynxTcpServerProfile profile)
            : this(null, profile.MessageClientFactory, profile.MessageRouter, profile.LoggerFactory, null)
        {
        }
    }

    /// <summary>
    /// Represents a TCP client socket connection to a <see cref="SphynxTcpServer"/>.
    /// </summary>
    public class SphynxTcpClient : ISphynxClient, IAsyncDisposable
    {
        /// <inheritdoc/>
        public Guid ClientId => Options.ClientId;

        /// <inheritdoc/>
        public IPEndPoint EndPoint { get; }

        /// <summary>
        /// Retrieves the running state of the client.
        /// </summary>
        public bool IsRunning => !_runTask?.IsCompleted ?? false;

        protected readonly SphynxTcpClientOptions Options;
        protected ILogger Logger = null!;
        private IDisposable? _loggerScope;
        protected SphynxMessageClient MessageClient = null!;

        private CancellationTokenSource _clientCts = new();
        private readonly AsyncLocal<bool> _isInsideRunTask = new();
        private readonly SemaphoreSlim _runLock = new(1, 1);
        private volatile Task? _runTask;
        private Socket _socket;
        private readonly NetworkStream _stream;

        protected bool IsDisposed => _state == 2;
        protected bool IsStopped => _state >= 1;

        // 0 = init, 1 = stopped, 2 = disposed
        private volatile int _state;

        public SphynxTcpClient(Socket socket, SphynxTcpClientOptions options)
        {
            _socket = socket;
            _stream = new NetworkStream(_socket, false);
            Options = options;
            EndPoint = (IPEndPoint)_socket.RemoteEndPoint!;
        }

        protected virtual void OnStarting()
        {
            Debug.Assert(Logger == null);
            Debug.Assert(MessageClient == null);

            Logger = Options.LoggerFactory.CreateLogger(GetType());
            _loggerScope = Logger.BeginScope(this);

            MessageClient = Options.MessageClientFactory(_stream);
            MessageClient.OnMessageReceived(static async void (state, msg) =>
            {
                var client = (SphynxTcpClient)state!;
                await client.RouteMessageAsync(msg, client._clientCts.Token).ConfigureAwait(false);
            }, this);
            MessageClient.OnMessageDropped(static async void (state, msg, ex) =>
            {
                var client = (SphynxTcpClient)state!;
                await client.OnMessageDroppedAsync(msg, ex).ConfigureAwait(false);
            });
        }

        private ValueTask OnMessageDroppedAsync(SphynxMessage? message, Exception? error = null)
        {
            // TODO: Log to metrics

            if (error != null)
            {
                if (Logger.IsEnabled(LogLevel.Error))
                {
                    if (message == null)
                        Logger.LogError(error, "An unexpected exception occured while reading a message");
                    else
                        Logger.LogError(error, "An unexpected exception occured while reading a message ({Message})", message.ToString());
                }
            }

            if (message == null)
                return ValueTask.CompletedTask;

            return DisposeMessageAsync(message);

            async ValueTask DisposeMessageAsync(SphynxMessage msg)
            {
                try
                {
                    if (msg is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    else if (msg is IDisposable disposable)
                        disposable.Dispose();
                }
                catch (Exception ex)
                {
                    if (Logger.IsEnabled(LogLevel.Trace))
                        Logger.LogTrace(ex, "An unexpected exception occured while disposing a dropped message ({Message})", msg.ToString());
                }
            }
        }

        public void Start(CancellationToken cancellationToken)
        {
            ThrowIfStopped();
            cancellationToken.ThrowIfCancellationRequested();

            _ = RunAsync(cancellationToken);
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfStopped();
            cancellationToken.ThrowIfCancellationRequested();

            // Prevent any accidental deadlocks
            if (_isInsideRunTask.Value)
            {
                Debug.Assert(_runTask != null);
                return;
            }

            using (await _runLock.RentAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_runTask != null)
                    // Don't propagate exceptions this time as we catch and log them all
                    return;

                ThrowIfStopped();

                OnStarting();

                Logger.LogDebug("Starting client read loop...");
                await RunAsyncInternal(cancellationToken).ConfigureAwait(false);
                Logger.LogDebug("Stopping client read loop...");
            }

            await StopAsync(_runTask?.Exception?.GetBaseException()).ConfigureAwait(false);
        }

        [MemberNotNull(nameof(_runTask))]
        private async Task RunAsyncInternal(CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled && !_clientCts.IsCancellationRequested)
                _clientCts = CancellationTokenSource.CreateLinkedTokenSource(_clientCts.Token, cancellationToken);

            try
            {
                try
                {
                    _isInsideRunTask.Value = true;
                    await (_runTask = ReadMessagesAsync(_clientCts.Token)).ConfigureAwait(false);
                }
                finally
                {
                    _isInsideRunTask.Value = false;
                }
            }
            catch (Exception ex) when (_clientCts.IsCancellationRequested)
            {
                // Client stopped
                // ReSharper disable once NonAtomicCompoundOperator
                _runTask ??= Task.FromException(ex);
            }
            catch (Exception ex)
            {
                // ReSharper disable once NonAtomicCompoundOperator
                _runTask ??= Task.FromException(ex);
                Logger.LogError(ex, "An unhandled exception occured during client execution");
            }
        }

        private Task ReadMessagesAsync(CancellationToken cancellationToken)
        {
            return MessageClient.RunAsync(cancellationToken);
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        private async ValueTask RouteMessageAsync(SphynxMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await Options.MessageRouter.ExecuteAsync(this, message, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logger.IsEnabled(LogLevel.Error))
                    Logger.LogError(ex, "Unhandled exception in message pipeline for message ({Message})", message.ToString());

                await OnMessageDroppedAsync(message).ConfigureAwait(false);
            }
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        public async ValueTask SendAsync(SphynxMessage message, CancellationToken cancellationToken = default)
        {
            ThrowIfStopped();

            try
            {
                // Don't think creating a CTS for each send operation would be very wise. Simply passing
                // the provided cancellationToken should be fine; in the worse case, the MessageClient
                // throws when trying to write to the underlying stream if this client has already started
                // its disposal process.
                await MessageClient.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logger.IsEnabled(LogLevel.Error))
                    Logger.LogError(ex, "An unexpected exception occured while sending a message ({Message})", message.ToString());

                await OnMessageDroppedAsync(message).ConfigureAwait(false);
            }
        }

        private void ThrowIfStopped()
        {
            ThrowIfDisposed();

            if (IsStopped)
                ThrowStoppedException();

            [DoesNotReturn]
            [StackTraceHidden]
            void ThrowStoppedException() => throw GetStopException();
        }

        private Exception GetStopException() => new OperationCanceledException("The client was stopped.",
            _clientCts.IsCancellationRequested ? _clientCts.Token : new CancellationToken(true));

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
        }

        /// <inheritdoc/>
        public ValueTask StopAsync(Exception? disconnectException = null, bool waitForFinish = true)
        {
            // We allow the client to be stopped even when disposed. Just makes our lives easier.
            if (IsStopped)
                return ValueTask.CompletedTask;

            // Try and reserve ourselves
            if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            {
                if (waitForFinish && !_isInsideRunTask.Value)
                    return WaitAsync();

                return ValueTask.CompletedTask;
            }

            return DisconnectAsync(disconnectException, waitForFinish);
        }

        private ValueTask DisconnectAsync(Exception? disconnectException, bool waitForFinish)
        {
            // Signal for stop
            if (!_clientCts.IsCancellationRequested)
            {
                try
                {
                    _clientCts.Cancel();
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

            if (!waitForFinish || _isInsideRunTask.Value)
            {
                ThreadPoolHelper.QueueUserWorkItem(static async void (state) =>
                {
                    try
                    {
                        await state.client.DisconnectSocketAsync(state.disconnectException).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        state.client.Logger.LogError(ex, "An exception occured while disconnecting the client");
                    }
                }, (client: this, disconnectException));

                return ValueTask.CompletedTask;
            }

            return DisconnectSocketAsync(disconnectException);
        }

        private async ValueTask DisconnectSocketAsync(Exception? disconnectException = null)
        {
            Debug.Assert(IsStopped);

            await WaitAsync().ConfigureAwait(false);

            try
            {
                await _socket.DisconnectAsync(true).ConfigureAwait(false);
                Logger.LogInformation("The client socket was disconnected");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An exception occured while disconnecting the client");
            }

            try
            {
                await OnDisconnectAsync(disconnectException).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }

            try
            {
            if (Options.OnDisconnect != null)
                await Options.OnDisconnect.Invoke(this, disconnectException).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// Called once the underlying socket has been disconnected.
        /// </summary>
        /// <param name="disconnectException">The disconnection exception, or null if it was a graceful disconnection.</param>
        protected virtual ValueTask OnDisconnectAsync(Exception? disconnectException)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Waits for the client to finish execution.
        /// </summary>
        private async ValueTask WaitAsync()
        {
            Debug.Assert(!_isInsideRunTask.Value);

            if (IsDisposed)
                return;

            if (!IsRunning)
                return;

            await _runLock.WaitAsync().ConfigureAwait(false);
            _runLock.Release();
        }

        /// <summary>
        /// Detaches the internal client socket so that it can be reused.
        /// </summary>
        public Socket? DetachSocket()
        {
            if (!IsStopped || _socket.Connected)
                throw new InvalidOperationException("Client must be stopped before detaching its socket");

            return Interlocked.Exchange(ref _socket, null!);
        }

        /// <summary>
        /// Asynchronously disposes of all resources held by this <see cref="SphynxTcpClient"/>.
        /// </summary>
        public virtual async ValueTask DisposeAsync()
        {
            if (IsDisposed)
                return;

            if (_isInsideRunTask.Value)
                throw new InvalidOperationException($"Cannot dispose from within the client. Call {nameof(StopAsync)}() instead.");

            await StopAsync(waitForFinish: true).ConfigureAwait(false);
            await DisposeClientAsync().ConfigureAwait(false);
        }

        private async ValueTask DisposeClientAsync()
        {
            Debug.Assert(IsStopped, "Client should have been stopped before disposing");

            if (Interlocked.Exchange(ref _state, 2) == 2)
                return;

            try
            {
                await _stream.DisposeAsync().ConfigureAwait(false);
                await MessageClient.DisposeAsync().ConfigureAwait(false);
                _loggerScope?.Dispose();

                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                _socket?.Dispose();
            }
            catch
            {
                // ignore
            }
        }

        private string? _scopeString;
        public override string ToString() => _scopeString ??= $"{EndPoint} ({ClientId})";
    }
}
