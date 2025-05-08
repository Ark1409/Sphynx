// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;

namespace Sphynx.Server.Auth.Handlers
{
    public class RegisterHandler : IPacketHandler<RegisterRequest>
    {
        public ValueTask HandlePacketAsync(SphynxClient client, RegisterRequest request, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                return client.SendPacketAsync(new LoginResponse(SphynxErrorCode.INVALID_USERNAME), token);

            if (string.IsNullOrWhiteSpace(request.Password))
                return client.SendPacketAsync(new LoginResponse(SphynxErrorCode.INVALID_PASSWORD), token);

            return HandleRegisterAsync(client, request, token);
        }

        private ValueTask HandleRegisterAsync(SphynxClient client, RegisterRequest request, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
