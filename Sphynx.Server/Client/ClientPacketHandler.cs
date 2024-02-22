using System.Diagnostics;
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
        public Task<bool> HandlePacketAsync(SphynxPacket packet)
        {
            switch (packet.PacketType)
            {
                case SphynxPacketType.LOGIN_REQ:
                    return HandleLoginRequestAsync((LoginRequestPacket)packet);

                case SphynxPacketType.LOGOUT_REQ:
                    return HandleLogoutRequestAsync((LogoutRequestPacket)packet);

                case SphynxPacketType.MSG_REQ:
                    return HandleMessageRequestAsync((MessageRequestPacket)packet);

                case SphynxPacketType.NOP:
                    return Task.FromResult(false);

                default:
                    return Task.FromResult(false);
            }
        }

        private async Task<bool> HandleLoginRequestAsync(LoginRequestPacket request)
        {
            var loginInfo = await SphynxClientManager.AuthenticateClient(_client, new SphynxUserCredentials(request.UserName, request.Password));
            var loginResponse = new LoginResponsePacket(loginInfo.ErrorCode);

            return await _client.SendPacketAsync(loginResponse);
        }

        private Task<bool> HandleLogoutRequestAsync(LogoutRequestPacket packet)
        {
            if (!VerifySession(packet.SessionId))
            {
                return _client.SendPacketAsync(new LogoutResponsePacket(SphynxErrorCode.INVALID_SESSION));
            }
            
            Debug.Assert(SphynxClientManager.UnauthenticateClient(_client));
            return _client.SendPacketAsync(new LogoutResponsePacket());
        }

        private async Task<bool> HandleMessageRequestAsync(MessageRequestPacket request)
        {
            throw new NotImplementedException();
        }

        private bool VerifySession(Guid sessionId)
        {
            return SphynxClientManager.TryGetSessionId(_client, out var actualSession) && sessionId == actualSession;
        }
    }
}