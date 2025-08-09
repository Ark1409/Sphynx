// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        /// The default <see cref="Backlog"/> size.
        /// </summary>
        public const int DEFAULT_BACKLOG_SIZE = 64;

        /// <summary>
        /// The default <see cref="BufferSize"/>.
        /// </summary>
        public const int DEFAULT_BUFFER_SIZE = 8192;

        /// <summary>
        /// The maximum number of clients in server backlog.
        /// </summary>
        public int Backlog { get; set; }

        /// <summary>
        /// Returns buffer size for information exchange.
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Retrieves the default server logging instance.
        /// </summary>
        public override ILogger Logger => _logger ??= LoggerFactory.CreateLogger(typeof(SphynxTcpServer));

        private ILogger _logger;

        /// <summary>
        /// The packet transporter used to send data to and read data from clients.
        /// </summary>
        public virtual IPacketTransporter PacketTransporter { get; set; }

        /// <summary>
        /// The central packet router which, when invoked, initiates a full packet processing cycle.
        /// </summary>
        public virtual IPacketRouter PacketRouter { get; set; }

        public SphynxTcpServerProfile() : this(true)
        {
        }

        protected SphynxTcpServerProfile(bool configure = false) : base(configure)
        {
            if (configure)
                ConfigureServices();
        }

        private void ConfigureServices()
        {
            PacketRouter = new PacketRouter();
            PacketTransporter = new PacketTransporter(null!);
            BufferSize = DEFAULT_BUFFER_SIZE;
            Backlog = DEFAULT_BACKLOG_SIZE;
        }
    }
}
