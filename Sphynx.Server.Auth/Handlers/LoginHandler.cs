// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using Sphynx.Core;
using Sphynx.Network.PacketV2.Request;
using Sphynx.Network.PacketV2.Response;
using Sphynx.ServerV2.Auth;
using Sphynx.ServerV2.Persistence;

namespace Sphynx.Server.Auth.Handlers
{
    public class LoginHandler : IPacketHandler<LoginRequest>
    {
        private readonly IUserRepository _userRepository;

        public LoginHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public ValueTask HandlePacketAsync(SphynxClient client, LoginRequest request, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                return client.SendPacketAsync(new LoginResponse(SphynxErrorCode.INVALID_USERNAME), token);

            if (string.IsNullOrWhiteSpace(request.Password))
                return client.SendPacketAsync(new LoginResponse(SphynxErrorCode.INVALID_PASSWORD), token);

            return HandleLoginAsync(client, request, token);
        }

        private async ValueTask HandleLoginAsync(SphynxClient client, LoginRequest request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var credentials = new SphynxUserCredentials(request.UserName, request.Password);
            var selfData = await _userRepository.GetSelfAsync(credentials.UserName, token);

            if (selfData.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendPacketAsync(new LoginResponse(selfData.ErrorCode), token);
                return;
            }

            Debug.Assert(selfData.Data is SphynxSelfInfo);

            var selfInfo = (SphynxSelfInfo)selfData.Data;

            // TODO: Verify password
            if (selfInfo.Password == credentials.Password)
            {
                await client.SendPacketAsync(new LoginResponse(SphynxErrorCode.INVALID_PASSWORD), token);
                return;
            }

            // TODO: Maybe store this session id
            await client.SendPacketAsync(new LoginResponse(selfInfo, Guid.NewGuid()), token);
        }
    }
}
