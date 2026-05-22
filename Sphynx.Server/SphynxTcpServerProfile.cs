// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using Microsoft.Extensions.Logging;
using Sphynx.Network;
using Sphynx.Network.Packet;
using Sphynx.Network.Serialization;
using Sphynx.Network.Serialization.MessagePack;
using Sphynx.Network.Transport;
using Sphynx.Server.Infrastructure.Routing;

namespace Sphynx.Server
{
    public delegate SphynxMessageClient MessageClientFactory(Stream stream);

    /// <summary>
    /// A profile which can be used to configure a <see cref="SphynxTcpServer"/>.
    /// </summary>
    public class SphynxTcpServerProfile : SphynxServerProfile
    {
        /// <summary>
        /// Returns the default <see cref="SphynxTcpServerProfile"/> which can be used to configure a <see cref="SphynxTcpServer"/>.
        /// </summary>
        public static SphynxTcpServerProfile Default => new();

        /// <summary>
        /// The default <see cref="Backlog"/> size.
        /// </summary>
        public const int DEFAULT_BACKLOG_SIZE = 64;

        /// <summary>
        /// The default <see cref="BufferSize"/>.
        /// </summary>
        public const int DEFAULT_BUFFER_SIZE = short.MaxValue; // 32KB

        /// <summary>
        /// The maximum number of clients in server backlog.
        /// </summary>
        public int Backlog { get; set; }

        /// <summary>
        /// Returns buffer size for information exchange.
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// A factory for <see cref="SphynxChannel"/>s that are used to send data.
        /// </summary>
        public MessageClientFactory MessageClientFactory { get; set; }

        /// <summary>
        /// The central <see cref="SphynxMessage"/> router which, when invoked, initiates a full message processing cycle.
        /// </summary>
        public IMessageRouter MessageRouter { get; set; }

        private readonly object _syncLock = new();
        private bool _configured;

        public SphynxTcpServerProfile()
        {
        }

        public override bool ConfigureProfile()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            lock (_syncLock)
            {
                if (_configured)
                    return false;

                LoggerFactory ??= GetDefaultLoggerFactory();
                Logger ??= LoggerFactory.CreateLogger(typeof(SphynxTcpServer));

                if (!base.ConfigureProfile())
                    return false;

                if (BufferSize == 0)
                    BufferSize = DEFAULT_BUFFER_SIZE;

                if (Backlog == 0)
                    Backlog = DEFAULT_BACKLOG_SIZE;

                MessageRouter ??= GetDefaultMessageRouter();
                MessageClientFactory ??= GetDefaultChannelFactory();

                return _configured = true;
            }
        }

        public static IMessageRouter GetDefaultMessageRouter() => new MessageRouter();

        private static readonly SphynxMessageFormatter<SphynxMessage> _defaultFormatter = new(null);
        private static IMessageFormatter DefaultSerializer => new SphynxMessageSerializer().WithFormatter(_defaultFormatter);

        public static MessageClientFactory GetDefaultChannelFactory(IMessageFormatter formatter) =>
            stream => new SphynxMessageClient(stream, false, formatter);

        public static MessageClientFactory GetDefaultChannelFactory() =>
            stream => new SphynxMessageClient(stream, false, DefaultSerializer);

    }
}
