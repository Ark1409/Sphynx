// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Sockets;

namespace Sphynx.ServerV2.Utils
{
    /// <summary>
    /// Helper class for transient-related errors.
    /// </summary>
    internal static class TransientHelper
    {
        /// <summary>
        /// Default transient error retry count.
        /// </summary>
        public const int RETRY_COUNT = 3;

        /// <summary>
        /// The default delay between the first invocation and the first retry (2nd invocation). The retry delay doubles
        /// from then on.
        /// </summary>
        public const int RETRY_DELAY_MS = 1000;

        /// <summary>
        /// Invokes the specified <paramref name="action"/> a maximum of <paramref name="maxAttempts"/> times,
        /// retrying on <see cref="IsTransient">transient errors</see> with a specified <paramref name="retryDelayMs"/>.
        /// </summary>
        /// <param name="action">The action to (possibly repeatedly) invoke.</param>
        /// <param name="onTransientError">The action to invoke when a transient error occurs.</param>
        /// <param name="maxAttempts">The maximum number of invocations.</param>
        /// <param name="retryDelayMs">The default delay between the first invocation and the first retry (2nd invocation).
        /// The retry delay doubles from then on.</param>
        /// <exception cref="AggregateException">An exception which contains all the transient errors, if any.</exception>
        public static void Invoke(Action action, Action<Exception>? onTransientError = null,
            int maxAttempts = RETRY_COUNT, int retryDelayMs = RETRY_DELAY_MS)
        {
            List<Exception>? transientErrors = null;

            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex) when (ex.IsTransient())
                {
                    transientErrors ??= new List<Exception>();
                    transientErrors.Add(ex);

                    onTransientError?.Invoke(ex);
                }

                Thread.Sleep(retryDelayMs * (1 << i));
            }

            throw new AggregateException(transientErrors!);
        }

        /// <summary>
        /// Invokes the specified asynchronous function a maximum of <paramref name="maxAttempts"/> times,
        /// retrying on <see cref="IsTransient">transient errors</see>.
        /// </summary>
        /// <param name="func">The function to (possibly repeatedly) invoke.</param>
        /// <param name="onTransientError">The action to invoke when a transient error occurs.</param>
        /// <param name="maxAttempts">The maximum number of invocations.</param>
        /// <param name="retryDelayMs">The default delay between the first invocation and the first retry (2nd invocation).
        /// The retry delay doubles from then on.</param>
        /// <exception cref="AggregateException">An exception which contains all the transient errors, if any.</exception>
        public static async Task InvokeAsync(Func<Task> func, Action<Exception>? onTransientError = null,
            int maxAttempts = RETRY_COUNT, int retryDelayMs = RETRY_DELAY_MS)
        {
            List<Exception>? transientErrors = null;

            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    await func().ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (ex.IsTransient())
                {
                    transientErrors ??= new List<Exception>();
                    transientErrors.Add(ex);

                    onTransientError?.Invoke(ex);
                }

                await Task.Delay(retryDelayMs * (1 << i)).ConfigureAwait(false);
            }

            throw new AggregateException(transientErrors!);
        }

        /// <summary>
        /// Invokes the specified asynchronous function a maximum of <paramref name="maxAttempts"/> times,
        /// retrying on <see cref="IsTransient">transient errors</see>.
        /// </summary>
        /// <typeparam name="TState">The data type to be used by <paramref name="func"/>.</typeparam>
        /// <param name="func">The function to (possibly repeatedly) invoke.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="func"/>.</param>
        /// <param name="maxAttempts">The maximum number of invocations.</param>
        /// <param name="retryDelayMs">The default delay between the first invocation and the first retry (2nd invocation).
        /// The retry delay doubles from then on.</param>
        /// <exception cref="AggregateException">An exception which contains all the transient errors, if any.</exception>
        public static async Task InvokeAsync<TState>(Func<TState, Task> func, TState state,
            int maxAttempts = RETRY_COUNT, int retryDelayMs = RETRY_DELAY_MS)
        {
            List<Exception>? transientErrors = null;

            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    await func(state).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (ex.IsTransient())
                {
                    transientErrors ??= new List<Exception>();
                    transientErrors.Add(ex);
                }

                await Task.Delay(retryDelayMs * (1 << i)).ConfigureAwait(false);
            }

            throw new AggregateException(transientErrors!);
        }

        /// <summary>
        /// Invokes the specified asynchronous function a maximum of <paramref name="maxAttempts"/> times,
        /// retrying on <see cref="IsTransient">transient errors</see>.
        /// </summary>
        /// <typeparam name="TState">The data type to be used by <paramref name="func"/>.</typeparam>
        /// <param name="func">The function to (possibly repeatedly) invoke.</param>
        /// <param name="onTransientError">The action to invoke when a transient error occurs.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="func"/>.</param>
        /// <param name="maxAttempts">The maximum number of invocations.</param>
        /// <param name="retryDelayMs">The default delay between the first invocation and the first retry (2nd invocation).
        /// The retry delay doubles from then on.</param>
        /// <exception cref="AggregateException">An exception which contains all the transient errors, if any.</exception>
        public static async Task InvokeAsync<TState>(Func<TState, Task> func, Action<TState, Exception>? onTransientError, TState state,
            int maxAttempts = RETRY_COUNT, int retryDelayMs = RETRY_DELAY_MS)
        {
            List<Exception>? transientErrors = null;

            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    await func(state).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (ex.IsTransient())
                {
                    transientErrors ??= new List<Exception>();
                    transientErrors.Add(ex);

                    // TODO: Maybe APM -> TPL?
                    onTransientError?.Invoke(state, ex);
                }

                await Task.Delay(retryDelayMs * (1 << i)).ConfigureAwait(false);
            }

            throw new AggregateException(transientErrors!);
        }

        /// <summary>
        /// Invokes the specified asynchronous function a maximum of <paramref name="maxAttempts"/> times,
        /// retrying on <see cref="IsTransient">transient errors</see>.
        /// </summary>
        /// <param name="func">The function to (possibly repeatedly) invoke.</param>
        /// <param name="onTransientError">The action to invoke when a transient error occurs.</param>
        /// <param name="state">An object containing data to be used by the <paramref name="func"/>.</param>
        /// <param name="maxAttempts">The maximum number of invocations.</param>
        /// <param name="retryDelayMs">The default delay between the first invocation and the first retry (2nd invocation).
        /// The retry delay doubles from then on.</param>
        /// <exception cref="AggregateException">An exception which contains all the transient errors, if any.</exception>
        public static async Task<T> InvokeAsync<T>(Func<object?, Task<T>> func,
            Action<object?, Exception>? onTransientError = null,
            object? state = null,
            int maxAttempts = RETRY_COUNT, int retryDelayMs = RETRY_DELAY_MS)
        {
            List<Exception>? transientErrors = null;

            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    return await func(state).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex.IsTransient())
                {
                    transientErrors ??= new List<Exception>();
                    transientErrors.Add(ex);

                    // TODO: Maybe APM -> TPL?
                    onTransientError?.Invoke(state, ex);
                }

                await Task.Delay(retryDelayMs * (1 << i)).ConfigureAwait(false);
            }

            throw new AggregateException(transientErrors!);
        }

        /// <summary>
        /// Returns whether the exception can be considered a transient error.
        /// </summary>
        /// <param name="ex">The exception to investigate.</param>
        /// <returns>Whether the exception can be considered a transient error.</returns>
        public static bool IsTransient(this Exception ex)
        {
            SocketException? socketException = ex as SocketException ?? ex.InnerException as SocketException;

            if (socketException is null)
                return false;

            switch (socketException.SocketErrorCode)
            {
                case SocketError.NetworkDown:
                case SocketError.TryAgain:
                case SocketError.HostUnreachable:
                    return true;

                default:
                    return false;
            }
        }
    }
}
