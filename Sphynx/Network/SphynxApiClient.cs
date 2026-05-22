// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics;
using Sphynx.Network.Packet;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;
using Sphynx.Network.Serialization;
using Sphynx.Network.Serialization.MessagePack;
using Sphynx.Network.Transport;
using Sphynx.Utils;

namespace Sphynx.Network
{
    public class SphynxApiClient : IDisposable, IAsyncDisposable
    {
        private static readonly TimeSpan _maxResponseTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 1); // From Timer.cs
        private static readonly SphynxMessageFormatter<SphynxMessage> _defaultFormatter = new(null);
        private static SphynxMessageSerializer DefaultSerializer => new SphynxMessageSerializer().WithFormatter(_defaultFormatter);

        public IMessageFormatter MessageFormatter { get => _client.MessageFormatter; set => _client.MessageFormatter = value; }

        public TimeSpan ResponseTimeout
        {
            get => _responseTimeout;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, Timeout.InfiniteTimeSpan);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _maxResponseTimeout);
                _responseTimeout = value;
            }
        }

        private TimeSpan _responseTimeout = Timeout.InfiniteTimeSpan;

        public Guid SessionId { get; set; }

        private readonly ConcurrentDictionary<Guid, IOutgoingRequest> _outgoingRequests = new();
        private readonly SphynxMessageClient _client;
        private volatile bool _disposed;

        private volatile MessageDroppedHandler? _messageDropped;
        private object? _messageDroppedState;

        public SphynxApiClient(Stream stream, bool ownsStream = true)
            : this(stream, ownsStream, DefaultSerializer)
        {
        }

        public SphynxApiClient(Stream stream, bool ownsStream, IMessageFormatter messageFormatter)
            : this(new SphynxMessageClient(stream, ownsStream, messageFormatter))
        {
        }

        public SphynxApiClient(SphynxChannel channel, IMessageFormatter messageFormatter)
            : this(new SphynxMessageClient(channel, messageFormatter))
        {
        }

        private SphynxApiClient(SphynxMessageClient client)
        {
            Debug.Assert(client != null);
            _client = client;
            _client.OnMessageReceived(OnMessageReceived, this);
        }

        public void OnMessageDropped(MessageDroppedHandler callback, object? state = null)
        {
            _messageDroppedState = state;
            _messageDropped = callback;
        }

        public SphynxResponse SendRequest(SphynxRequest request, CancellationToken cancellationToken = default) =>
            SendRequestAsync(request, cancellationToken).GetAwaiter().GetResult();

        public TResponse SendRequest<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : SphynxRequest<TResponse>
            where TResponse : SphynxResponse =>
            SendRequestAsync<TRequest, TResponse>(request, cancellationToken).GetAwaiter().GetResult();

        public Task<SphynxResponse> SendRequestAsync(SphynxRequest request, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return Task.FromException<SphynxResponse>(GetDisposedException());

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<SphynxResponse>(cancellationToken);

            var outgoingRequest = new OutgoingRequest<SphynxResponse>(this, request, cancellationToken);
            outgoingRequest.BeginExchange();

            return outgoingRequest.Task;
        }

        public Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : SphynxRequest<TResponse>
            where TResponse : SphynxResponse
        {
            if (_disposed)
                return Task.FromException<TResponse>(GetDisposedException());

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<TResponse>(cancellationToken);

            var outgoingRequest = new OutgoingRequest<TResponse>(this, request, cancellationToken);
            outgoingRequest.BeginExchange();

            return outgoingRequest.Task;
        }

        public void Start(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _client.Start(cancellationToken);
        }

        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return Task.FromException(GetDisposedException());

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            return _client.RunAsync(cancellationToken);
        }

        private static async void OnMessageReceived(object? state, SphynxMessage message)
        {
            var client = (SphynxApiClient)state!;

            try
            {
                await client.OnMessageReceivedAsync(message).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }

        private ValueTask OnMessageReceivedAsync(SphynxMessage message)
        {
            // TODO: Perform this check at the Serializer level
            if (message is not SphynxResponse response)
                return DropMessageAsync(message);

            if (!_outgoingRequests.TryGetValue(response.Header.RequestId, out var outgoingRequest))
                return DropMessageAsync(response);

            if (!outgoingRequest.TryEndExchange(response))
                return DropMessageAsync(response);

            return ValueTask.CompletedTask;

            ValueTask DropMessageAsync(SphynxMessage msg)
            {
                if (InvokeMessageDropped(msg, null))
                    return ValueTask.CompletedTask;

                // Default behaviour for uncaught messages
                if (msg is IAsyncDisposable asyncDisposable)
                    return asyncDisposable.DisposeAsync();

                if (msg is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        return ValueTask.FromException(ex);
                    }
                }

                return ValueTask.CompletedTask;
            }
        }

        private bool InvokeMessageDropped(SphynxMessage? message, Exception? error)
        {
            var callback = _messageDropped;
            object? callbackState = _messageDroppedState;

            if (callback == null)
                return false;

            return ThreadPoolHelper.QueueUserWorkItem(static void (state) =>
            {
                try
                {
                    state.callback.Invoke(state.callbackState, state.message, state.error);
                }
                catch
                {
                    // ignore
                }
            }, (message, error, callback, callbackState));
        }

        private ObjectDisposedException GetDisposedException() => new(GetType().Name);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _client.Dispose();

            foreach (var (_, outgoingRequest) in _outgoingRequests)
                outgoingRequest.TryAbortExchange();

            _outgoingRequests.Clear();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            await _client.DisposeAsync().ConfigureAwait(false);

            foreach (var (_, outgoingRequest) in _outgoingRequests)
                outgoingRequest.TryAbortExchange();

            _outgoingRequests.Clear();
        }

        private interface IOutgoingRequest
        {
            SphynxRequest Request { get; }
            bool BeginExchange();
            bool TryEndExchange(SphynxResponse response);
            bool TryAbortExchange(string? reason = null);
        }

        private class OutgoingRequest<TResponse> : TaskCompletionSource<TResponse>, IOutgoingRequest
            where TResponse : SphynxResponse
        {
            public SphynxRequest Request { get; }

            private readonly SphynxApiClient _client;
            private readonly CancellationToken _cancellationToken;
            private Timer? _timer;

            public OutgoingRequest(SphynxApiClient client, SphynxRequest request, CancellationToken cancellationToken)
            {
                _client = client;
                _cancellationToken = cancellationToken;
                SetupRequest(Request = request);

                Task.ContinueWith(static (_, state) => ((OutgoingRequest<TResponse>)state!)._timer?.Dispose(), this,
                    TaskContinuationOptions.ExecuteSynchronously);
            }

            private void SetupRequest(SphynxRequest request)
            {
                request.Header.SessionId = _client.SessionId;
                request.Header.RequestId = Guid.NewGuid();
            }

            private static Timer NewTimer(IOutgoingRequest request, TimeSpan responseTimeout)
            {
                return new Timer(static state => ((IOutgoingRequest)state!).TryAbortExchange("Response timed out"),
                    request,
                    responseTimeout,
                    Timeout.InfiniteTimeSpan);
            }

            private ValueTask _requestTask;

            public bool BeginExchange()
            {
                if (!_requestTask.Equals(default) || Task.IsCompleted)
                    return false;

                if (!_client._outgoingRequests.TryAdd(Request.Header.RequestId, this))
                {
                    TrySetException(new InvalidOperationException($"Request with same ID is already pending ({Request.Header.RequestId})"));
                    return false;
                }

                try
                {
                    _requestTask = _client._client.SendMessageAsync(Request, _cancellationToken);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException oce)
                        TrySetCanceled(oce.CancellationToken);
                    else
                        TrySetException(ex);

                    return false;
                }

                if (_requestTask.IsCompleted)
                    StartResponseTimer();
                else
                    // We either allocate an Action here or a Task for the public API...
                    _requestTask.GetAwaiter().OnCompleted(StartResponseTimer);

                return true;
            }

            private void StartResponseTimer()
            {
                Debug.Assert(_requestTask != default);

                try
                {
                    _requestTask.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException oce)
                        TrySetCanceled(oce.CancellationToken);
                    else
                        TrySetException(ex);

                    return;
                }

                if (_client.ResponseTimeout != Timeout.InfiniteTimeSpan)
                    _timer = NewTimer(this, _client.ResponseTimeout);
            }

            public bool TryEndExchange(SphynxResponse response)
            {
                if (response.Header.RequestId != Request.Header.RequestId || response is not TResponse resp)
                    return false;

                if (!_client._outgoingRequests.TryRemove(new KeyValuePair<Guid, IOutgoingRequest>(response.Header.RequestId, this)))
                    return false;

                return TrySetResult(resp);
            }

            public bool TryAbortExchange(string? reason = null)
            {
                if (Task.IsCompleted)
                    return false;

                return TrySetException(new OperationCanceledException(reason, new CancellationToken(true)));
            }
        }
    }
}
