// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;

namespace Sphynx.Utils
{
    public static class ThreadPoolHelper
    {
        public static bool UseTaskScheduler = false;

        public static bool QueueUserWorkItem<TState>(Action<TState> callBack, TState state)
        {
            if (!UseTaskScheduler)
                return ThreadPool.QueueUserWorkItem(callBack, state, preferLocal: false);

            return QueueToTaskScheduler(callBack, state);
        }

        private static bool QueueToTaskScheduler<TState>(Action<TState> callBack, TState state)
        {
            if (typeof(TState) == typeof(object))
            {
                var action = Unsafe.As<Action<TState>, Action<object?>>(ref callBack);
                Task.Factory.StartNew(action, state, TaskCreationOptions.DenyChildAttach);
            }
            else
            {
                Task.Factory.StartNew(s => callBack((TState)s!), state, TaskCreationOptions.DenyChildAttach);
            }

            return true;
        }
    }
}
