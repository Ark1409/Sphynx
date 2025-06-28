// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Network.Transport;
using Sphynx.ServerV2.Infrastructure.Routing;

namespace Sphynx.ServerV2
{
    /// <summary>
    /// A profile which can be used to configure a <see cref="SphynxTcpServer"/>.
    /// </summary>
    public class SphynxTcpServerProfile : SphynxServerProfile
    {
        /// <summary>
        /// The maximum number of clients in server backlog.
        /// </summary>
        public int Backlog { get; set; } = 64;

        /// <summary>
        /// Returns buffer size for information exchange.
        /// </summary>
        public int BufferSize { get; protected set; } = 8192;

        /// <summary>
        /// Retrieves the default server logging instance.
        /// </summary>
        public override ILogger Logger => _logger ??= LoggerFactory.CreateLogger(typeof(SphynxTcpServer));

        private ILogger? _logger;

        /// <summary>
        /// The packet transporter used to send data to and read data from clients.
        /// </summary>
        public virtual IPacketTransporter PacketTransporter { get; set; } = new PacketTransporter();

        /// <summary>
        /// The central packet router which, when invoked, initiates a full packet processing cycle.
        /// </summary>
        public virtual IPacketRouter PacketRouter { get; set; } = new PacketRouter();
    }
}
