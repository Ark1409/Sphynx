// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace Sphynx.Utils
{
    public static class TaskExtensions
    {
        public static void Wait(this ref ValueTask vtask)
        {
            if (!vtask.IsCompleted)
            {
                var ev = new AutoResetEvent(false);
                vtask.GetAwaiter().OnCompleted(() => ev.Set());
                ev.WaitOne();
            }

            vtask.GetAwaiter().GetResult();
            vtask = ValueTask.CompletedTask;
        }

        public static bool Wait(this ref ValueTask vtask, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                Wait(ref vtask);
                return true;
            }

            if (!vtask.IsCompleted)
            {
                var ev = new AutoResetEvent(false);
                vtask.GetAwaiter().OnCompleted(() => ev.Set());
                if (!ev.WaitOne(timeout))
                {
                    return false;
                }
            }

            vtask.GetAwaiter().GetResult();
            vtask = ValueTask.CompletedTask;
            return true;
        }
        public static bool Wait<T>(this ref ValueTask<T> vtask, [MaybeNullWhen(false)] out T item)
        {
            if (!vtask.IsCompleted)
            {
                var ev = new AutoResetEvent(false);
                vtask.GetAwaiter().OnCompleted(() => ev.Set());
                ev.WaitOne();
            }

            try
            {
                item = vtask.Result;
                vtask = ValueTask.FromResult(item);
                return true;
            }
            catch
            {
                item = default;
                return false;
            }
        }

        public static bool Wait<T>(this ref ValueTask<T> vtask, [MaybeNullWhen(false)] out T item, TimeSpan timeout)
        {
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                return Wait(ref vtask, out item);
            }

            if (!vtask.IsCompleted)
            {
                var ev = new AutoResetEvent(false);
                vtask.GetAwaiter().OnCompleted(() => ev.Set());
                if (!ev.WaitOne(timeout))
                {
                    item = default;
                    return false;
                }
            }

            try
            {
                item = vtask.Result;
                vtask = ValueTask.FromResult(item);
                return true;
            }
            catch
            {
                item = default;
                return false;
            }
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public static async ValueTask<U> Select<U, T>(this ValueTask<T> vtask, Func<ValueTask<T>, ValueTask<U>> r)
        {
            ValueTask<T> vt;
            try
            {
                var v = await vtask;
                vt = ValueTask.FromResult(v);
            }
            catch (OperationCanceledException e)
            {
                vt = ValueTask.FromCanceled<T>(e.CancellationToken);
            }
            catch (Exception e)
            {
                vt = ValueTask.FromException<T>(e);
            }
            return await r(vt);
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public static async ValueTask<U> Select<U, T>(this ValueTask<T> vtask, Func<ValueTask<T>, U> r)
        {
            ValueTask<T> vt;
            try
            {
                var v = await vtask;
                vt = ValueTask.FromResult(v);
            }
            catch (OperationCanceledException e)
            {
                vt = ValueTask.FromCanceled<T>(e.CancellationToken);
            }
            catch (Exception e)
            {
                vt = ValueTask.FromException<T>(e);
            }
            return r(vt);
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public static async ValueTask<U> SelectResult<U, T>(this ValueTask<T> vtask, Func<T, ValueTask<U>> r)
        {
            return await r(await vtask);
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public static async ValueTask<U> SelectResult<U, T>(this ValueTask<T> vtask, Func<T, U> r)
        {
            return r(await vtask);
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public static async ValueTask<T[]> WhenAll<T>(IEnumerable<ValueTask<T>> tasks)
        {
            var arr = new T[tasks.Count()];
            int i = 0;
            foreach (var t in tasks)
            {
                var item = await t;
                arr[i] = item;
                i++;
            }
            return arr;
        }
        public static ValueTask<ValueTask<T>> WhenAny<T>(IEnumerable<ValueTask<T>> tasks)
        {
            var v = new ManualResetValueTaskSource<ValueTask<T>>();
            foreach (var t in tasks)
            {
                t.GetAwaiter().OnCompleted(() =>
                {
                    if (v.GetStatus() == ValueTaskSourceStatus.Pending)
                        v.SetResult(t);
                });
            }
            return v.ValueTask;
        }
    }
}
