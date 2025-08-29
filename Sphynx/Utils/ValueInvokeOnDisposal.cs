// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Sphynx.Utils
{
    public readonly struct ValueInvokeOnDisposal : IDisposable
    {
        private readonly Action _action;

        public ValueInvokeOnDisposal(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action.Invoke();
        }
    }

    public readonly struct ValueInvokeOnDisposal<T> : IDisposable
    {
        private readonly T _state;
        private readonly Action<T> _action;

        public ValueInvokeOnDisposal(T state, Action<T> action)
        {
            _state = state;
            _action = action;
        }

        public void Dispose()
        {
            _action.Invoke(_state);
        }
    }
}
