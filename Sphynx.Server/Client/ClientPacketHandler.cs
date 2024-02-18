using Sphynx.Packet;
using Sphynx.Packet.Request;
using Sphynx.Packet.Response;
using Sphynx.Server.User;
using Sphynx.Server.Utils;

namespace Sphynx.Server.Client
{
    /// <summary>
    /// Handles incoming client packets and performs appropriate actions depending on the packet that is being handled.
    /// </summary>
    public sealed class ClientPacketHandler : IPacketHandler
    {
        private readonly SphynxClient _client;

        /// <inheritdoc cref="ClientPacketHandler"/>
        public ClientPacketHandler(SphynxClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Asynchronously performs the appropriate actions for the given <paramref name="packet"/> request.
        /// </summary>
        /// <param name="packet">The packet to handle.</param>
        /// <returns>The started handling task, returning a bool representing whether the packet could be sent.</returns>
        public async Task<bool> HandlePacketAsync(SphynxPacket packet)
        {
            switch (packet.PacketType)
            {
                case SphynxPacketType.LOGIN_REQ:
                    return await HandleLoginRequestAsync((LoginRequestPacket)packet).ConfigureAwait(false);
                
                case SphynxPacketType.LOGOUT_REQ:
                    return await HandleLogoutRequestAsync((LogoutRequestPacket)packet).ConfigureAwait(false);

                case SphynxPacketType.MSG_REQ:
                    return await HandleMessageRequestAsync((MessageRequestPacket)packet).ConfigureAwait(false);

                case SphynxPacketType.NOP:
                    return true;

                default:
                    return false;
            }
        }

        private async Task<bool> HandleLoginRequestAsync(LoginRequestPacket request)
        {
            var loginInfo = await SphynxClientManager.AuthenticateClient(_client, new SphynxUserCredentials(request.UserName, request.Password));
            var loginResponse = new LoginResponsePacket(loginInfo.ErrorCode);

            return await _client.SendPacketAsync(loginResponse).ConfigureAwait(false);
        }
        
        private async Task<bool> HandleLogoutRequestAsync(LogoutRequestPacket packet)
        {
            if (SphynxClientManager.UnauthenticateClient(_client))
            {
                var logoutResponse = new LogoutResponsePacket();
                return await _client.SendPacketAsync(logoutResponse).ConfigureAwait(false);
            }

            return false;
        }

        private async Task<bool> HandleMessageRequestAsync(MessageRequestPacket request)
        {
            throw new NotImplementedException();
        }
    }
}