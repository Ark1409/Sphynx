// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Channels;

namespace Sphynx.Utils
{
    public static class ChannelExtensions
    {
        public static async ValueTask DrainAsync<T>(this ChannelReader<T> reader, Action<T>? drainAction = null,
            CancellationToken cancellationToken = default)
        {
            while (true)
            {
                try
                {
                    if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                        return;
                }
                catch
                {
                    return;
                }

                if (reader.TryRead(out var item))
                    drainAction?.Invoke(item);
            }
        }
    }
}
