// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;
using Sphynx.Server.Chat.Model;
using Sphynx.Server.Chat.Persistence;
using Sphynx.Server.Chat.Services;
using Sphynx.Server.Extensions;
using Sphynx.Server.Infrastructure.Services;

namespace Sphynx.Server.Chat.Handlers
{
    public class FetchUsersHandler : RequestHandler<GetUsersRequest, GetUsersResponse>
    {
        private readonly IUserService _userService;

        public FetchUsersHandler(ISessionService sessionService, IUserService userService, ILogger<FetchUsersHandler> logger)
            : base(sessionService, logger)
        {
            _userService = userService;
        }

        protected override async Task<GetUsersResponse> HandleRequestAsync(RequestContext ctx, CancellationToken cancellationToken = default)
        {
            var usersResult = await _userService.GetUsersAsync(ctx.Request.UserIds, cancellationToken).ConfigureAwait(false);

            if (usersResult.ErrorCode != SphynxErrorCode.SUCCESS)
                return new GetUsersResponse(usersResult.MaskServerError());

            var users = usersResult.Data!.Select(user => user.ToDto()).ToArray();
            return new GetUsersResponse(users);
        }
    }
}
