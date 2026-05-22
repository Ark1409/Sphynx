// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Logging;
using Sphynx.Core;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;
using Sphynx.Server.Auth;
using Sphynx.Server.Client;
using Sphynx.Server.Extensions;
using Sphynx.Server.Infrastructure.Handlers;
using Sphynx.Server.Infrastructure.Services;

namespace Sphynx.Server.Chat.Handlers
{
    // TODO: Put this class into Sphynx.Server (for request tag) (so auth server can use it)
    // TODO: Put server things into Core package (for chat and auth sser
    public abstract class RequestHandler<TRequest, TResponse> : IMessageHandler<TRequest>
        where TRequest : SphynxRequest<TResponse>
        where TResponse : SphynxResponse
    {
        private readonly ISessionService _sessionService;
        protected ILogger Logger { get; set; }

        public RequestHandler(ISessionService sessionService, ILogger logger)
        {
            _sessionService = sessionService;
            Logger = logger;
        }

        public async Task HandleMessageAsync(ISphynxClient client, TRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Header.SessionId == default)
            {
                await client.SendAsync(request.CreateResponse(SphynxErrorCode.INVALID_TOKEN), cancellationToken).ConfigureAwait(false);
                return;
            }

            // TODO: Write behind/through on intervals
            var reviveResult = await _sessionService
                .ReviveSessionAsync(request.Header.SessionId, SessionUpdatePolicy.Ephemeral, cancellationToken)
                .ConfigureAwait(false);

            if (reviveResult.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                await client.SendAsync(request.CreateResponse(reviveResult.MaskServerError()), cancellationToken).ConfigureAwait(false);
                return;
            }

            // TODO: If the client's IP Address doesn't match the SesionInfo's IPAddress, we've got something wrong (hacker?). Erorr out.
            //  (what about DHCP tho...)

            var requestContext = new RequestContext
            {
                Client = client,
                SessionInfo = reviveResult.Data!.Value,
                Request = request
            };

            await ProcessRequestAsync(requestContext, cancellationToken).ConfigureAwait(false);
        }

        private async Task ProcessRequestAsync(RequestContext context, CancellationToken cancellationToken)
        {
            SphynxResponse response;

            try
            {
                response = await HandleRequestAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Logger?.IsEnabled(LogLevel.Error) ?? false)
                    Logger.LogError(ex, "Request handling for packet {Packet} failed with exception", context.Request.MessageType);

                response = context.Request.CreateResponse(SphynxErrorCode.SERVER_ERROR);
            }

            // response.RequestTag = context.Request.RequestTag;

            await context.Client.SendAsync(response, cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task<TResponse> HandleRequestAsync(RequestContext ctx, CancellationToken cancellationToken = default);

        public readonly record struct RequestContext(ISphynxClient Client, SphynxSessionInfo SessionInfo, TRequest Request);
    }
}
