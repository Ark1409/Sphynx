// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using System.Text;

namespace Sphynx.Utils
{
    public static class EnumerableExtensions
    {
        public static DualEnumerator<T> ToAsyncEnumerable<T>(this IEnumerable<T> @enum)
        {
            return new DualEnumerator<T>(@enum);
        }

        public static string FromUtf32String(this IEnumerable<int> en)
        {
            var sb = new StringBuilder();
            foreach (var i in en)
            {
                sb.Append(char.ConvertFromUtf32(i));
            }
            return sb.ToString();
        }
    }

    public readonly struct DualEnumerator<T> : IEnumerable<T>, IAsyncEnumerable<T>
    {
        private readonly IEnumerable<T> Original { get; }

        public DualEnumerator(IEnumerable<T> original)
        {
            Original = original;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Original.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Original.GetEnumerator();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumerator(((IEnumerable<T>)this).GetEnumerator(), cancellationToken);
        }

        private readonly struct AsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _enum;
            private readonly CancellationToken _token;

            public AsyncEnumerator(IEnumerator<T> @enum, CancellationToken token)
            {
                _enum = @enum;
                _token = token;
            }

            public T Current => _enum.Current;
            public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(!_token.IsCancellationRequested && _enum.MoveNext());

            public ValueTask DisposeAsync()
            {
                _enum.Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
