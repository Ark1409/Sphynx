// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// https://github.com/dotnet/dotnet/blob/9037d9f7393290ef7c04983ea6a0799b3a210a3c/src/aspnetcore/src/Shared/ServerInfrastructure/ManualResetValueTaskSource.cs
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks.Sources;

namespace Sphynx.Utils
{
    public sealed class ManualResetValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> _core; // mutable struct; do not make this readonly

        public bool RunContinuationsAsynchronously { get => _core.RunContinuationsAsynchronously; set => _core.RunContinuationsAsynchronously = value; }
        public short Version => _core.Version;
        public void SetResult(T result) => _core.SetResult(result);
        public void SetException(Exception error) => _core.SetException(error);

        public T GetResult(short token)
        {
            var v = _core.GetResult(token);
            Reset();
            return v;
        }

        void IValueTaskSource.GetResult(short token)
        {
            _core.GetResult(token);
            Reset();
        }
        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);

        public ValueTaskSourceStatus GetStatus() => _core.GetStatus(_core.Version);

        public ValueTask<T> ValueTask => new(this, Version);

        public void Reset() => _core.Reset();
    }
}
