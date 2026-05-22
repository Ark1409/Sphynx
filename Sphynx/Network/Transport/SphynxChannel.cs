// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Sphynx.Storage;

namespace Sphynx.Network.Transport
{
    public class SphynxChannel : IDisposable, IAsyncDisposable
    {
        public SphynxChannelWriter Writer { get; protected init; }
        public SphynxChannelReader Reader { get; protected init; }

        /// <summary>
        /// The running task for this channel. The channel should be <see cref="RunAsync">ran</see> before the
        /// <see cref="Writer">writer</see> is accessed.
        /// </summary>
        public Task? RunTask => Reader.RunTask;

        public SphynxChannel(Stream stream, bool ownsStream = false)
            : this(new DefaultChannelWriter(stream, ownsStream), new DefaultChannelReader(stream))
        {
            ConnectChannel((DefaultChannelWriter)Writer, (DefaultChannelReader)Reader);
        }

        protected SphynxChannel(SphynxChannelWriter writer, SphynxChannelReader reader)
        {
            Writer = writer;
            Reader = reader;
        }

        protected internal static void ConnectChannel(DefaultChannelWriter writer, DefaultChannelReader reader)
        {
            reader.OnChannelReleasing(static (state, channelId, flags) =>
            {
                var writer = (DefaultChannelWriter)state!;
                return writer.SendReleaseAsync(channelId, flags);
            }, writer);

            reader.OnChannelReleased(static (state, channelId, flags) =>
            {
                var writer = (DefaultChannelWriter)state!;
                writer.OnChannelReleased(channelId, flags);
                return ValueTask.CompletedTask;
            }, writer);
        }

        /// <summary>
        /// Starts the reader for this channel. This should be called before the <see cref="Writer">writer</see> is accessed.
        /// </summary>
        [MemberNotNull(nameof(RunTask))]
        public void Start(CancellationToken cancellationToken = default)
        {
            Reader.Start(cancellationToken);
            Debug.Assert(RunTask != null);
        }

        /// <summary>
        /// Starts the reader for this channel. This should be called before the <see cref="Writer">writer</see> is accessed.
        /// </summary>
        /// <exception cref="Exception">The exception which terminated the reading.</exception>
        [MemberNotNull(nameof(RunTask))]
        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            var runTask = Reader.RunAsync(cancellationToken);
            Debug.Assert(RunTask != null);

            return runTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Writer.Dispose();
                Reader.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            await Writer.DisposeAsync().ConfigureAwait(false);
            await Reader.DisposeAsync().ConfigureAwait(false);
        }
    }

    public class PoolableSphynxChannel : SphynxChannel
    {
        public PoolableSphynxChannel(Stream stream, SequencePool? writerPool = null, IObjectPool<Pipe>? readerPool = null, bool ownsStream = false)
            : base(new PoolableChannelWriter(stream, writerPool, ownsStream), new PoolableChannelReader(stream, readerPool))
        {
            ConnectChannel((PoolableChannelWriter)Writer, (PoolableChannelReader)Reader);
        }

        public PoolableSphynxChannel(Stream stream, int channelPoolSize, SequencePool? writerPool = null, IObjectPool<Pipe>? readerPool = null,
            bool ownsStream = false)
            : base(
                new PoolableChannelWriter(stream, channelPoolSize, writerPool, ownsStream),
                new PoolableChannelReader(stream, channelPoolSize, readerPool, ownsStream)
            )
        {
            ConnectChannel((PoolableChannelWriter)Writer, (PoolableChannelReader)Reader);
        }

        public void Reset(Stream stream, bool? ownsStream = null)
        {
            var writer = (PoolableChannelWriter)Writer;
            var reader = (PoolableChannelReader)Reader;

            writer.Reset(stream, ownsStream);
            reader.Reset(stream);

            ConnectChannel(writer, reader);
        }
    }
}
