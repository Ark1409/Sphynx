using System.Buffers;
using System.Diagnostics;
using Sphynx.Core;
using Sphynx.Model.ChatRoom;
using Sphynx.Network.Packet;
using Sphynx.Network.Packet.Broadcast;
using Sphynx.Network.Packet.Request;
using Sphynx.Network.Packet.Response;
using Sphynx.Server.ChatRoom;
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
            // TODO: maybe perform null checks on every field u deserialize since user might be crazy
            // TODO: cant let sever error too...
            // TODO: 
            switch (packet.PacketType)
            {
                case SphynxPacketType.LOGIN_REQ:
                    return HandleLoginRequestAsync((LoginRequestPacket)packet);

                case SphynxPacketType.LOGOUT_REQ:
                    return HandleLogoutRequestAsync((LogoutRequestPacket)packet);

                case SphynxPacketType.MSG_REQ:
                    return HandleMessageRequestAsync((MessageRequestPacket)packet);

                case SphynxPacketType.ROOM_CREATE_REQ:
                    return HandleRoomCreateRequestAsync((RoomCreateRequestPacket)packet);

                case SphynxPacketType.ROOM_DEL_REQ:
                    return HandleRoomDeleteRequestAsync((RoomDeleteRequestPacket)packet);

                case SphynxPacketType.NOP:
                    return Task.FromResult(false);

                default:
                    return Task.FromResult(false);
            }
        }

        private async Task<bool> HandleLoginRequestAsync(LoginRequestPacket request)
        {
            var loginInfo = await SphynxClientManager.AuthenticateClient(_client, new SphynxUserCredentials(request.UserName, request.Password));
            LoginResponsePacket loginResponse;

            if (loginInfo)
            {
                var (user, sessionId) = loginInfo.Data;
                loginResponse = new LoginResponsePacket(user!, sessionId);
            }
            else
            {
                loginResponse = new LoginResponsePacket(loginInfo.ErrorCode);
            }

            return await _client.SendPacketAsync(loginResponse);
        }

        private Task<bool> HandleLogoutRequestAsync(LogoutRequestPacket request)
        {
            if (!VerifySession(request.SessionId))
            {
                return _client.SendPacketAsync(new LogoutResponsePacket(SphynxErrorCode.INVALID_SESSION));
            }

            bool logoutInfo = SphynxClientManager.UnauthenticateClient(_client);
            var logoutResponse = new LogoutResponsePacket(logoutInfo ? SphynxErrorCode.SUCCESS : SphynxErrorCode.INVALID_USER);

            return _client.SendPacketAsync(logoutResponse);
        }

        private async Task<bool> HandleMessageRequestAsync(MessageRequestPacket request)
        {
            if (!VerifySession(request.SessionId))
            {
                return await _client.SendPacketAsync(new MessageResponsePacket(SphynxErrorCode.INVALID_SESSION));
            }

            // Update db
            var msgInfo = await SphynxRoomManager.AddMessageAsync(request.RoomId, request.UserId, request.Message);
            if (!msgInfo) return await _client.SendPacketAsync(new MessageResponsePacket(msgInfo.ErrorCode));

            // Inform sender of success
            // Retry mechanism should already be implemented in the client
            _client.SendPacketAsync(new MessageResponsePacket()).SafeBackgroundExecute();

            // Inform recipient(s) of message
            var recipientsInfo = await SphynxRoomManager.GetRoomFieldAsync<Guid[]>(request.RoomId, ChatRoomDbInfo.USERS_FIELD);
            if (!recipientsInfo) return await _client.SendPacketAsync(new MessageResponsePacket(recipientsInfo.ErrorCode));

            var recipients = recipientsInfo.Data!;

            for (int i = 0; i < recipients.Length; i++)
            {
                if (SphynxClientManager.TryGetClients(recipients[i], out var clients))
                {
                    // Broadcast to all client connections
                    foreach (var client in clients)
                    {
                        // Client will handle retries on failure
                        client.SendPacketAsync(new MessageBroadcastPacket(request.RoomId, msgInfo.Data!.MessageId)).SafeBackgroundExecute();
                    }
                }
                else
                {
                    // TODO: Increment "unread messages" counter in user profile
                    // TODO: Push back into array
                    // TODO: maybe dont put this in this else; might wanna mark it as pending as long as it has not been laoded/read
                    // SphynxUserManager.UpdateUserFieldAsync(_client.UserId!,, msgInfo.Data.MessageId);
                }
            }

            return true;
        }

        private Task<bool> HandleRoomCreateRequestAsync(RoomCreateRequestPacket request)
        {
            if (!VerifySession(request.SessionId))
            {
                return _client.SendPacketAsync(new RoomCreateResponsePacket(SphynxErrorCode.INVALID_SESSION));
            }

            switch (request.RoomType)
            {
                case ChatRoomType.DIRECT_MSG:
                    return HandleDirectRoomCreateRequestAsync((RoomCreateRequestPacket.Direct)request);
                case ChatRoomType.GROUP:
                    return HandleGroupRoomCreateRequestAsync((RoomCreateRequestPacket.Group)request);
            }

            return _client.SendPacketAsync(new RoomCreateResponsePacket(SphynxErrorCode.INVALID_ROOM));

            // TODO: create room and add user to it
        }

        private async Task<bool> HandleDirectRoomCreateRequestAsync(RoomCreateRequestPacket.Direct request)
        {
            var roomInfo = await SphynxRoomManager.CreateDirectRoomAsync(request.UserId, request.OtherId);
            var creationResponse = roomInfo ? new RoomCreateResponsePacket(roomInfo.Data!.RoomId) : new RoomCreateResponsePacket(roomInfo.ErrorCode);
            return await _client.SendPacketAsync(creationResponse);

            // TODO: create room and add user to it
        }

        private async Task<bool> HandleGroupRoomCreateRequestAsync(RoomCreateRequestPacket.Group request)
        {
            // TODO: u forgor to serialize isPublic
            var roomInfo = await SphynxRoomManager.CreateGroupRoomAsync(request.UserId, request.Name, request.Public, request.Password);
            var creationResponse = roomInfo ? new RoomCreateResponsePacket(roomInfo.Data!.RoomId) : new RoomCreateResponsePacket(roomInfo.ErrorCode);
            return await _client.SendPacketAsync(creationResponse);

            // TODO: make sure users are added to the room
        }

        private async Task<bool> HandleRoomDeleteRequestAsync(RoomDeleteRequestPacket request)
        {
            if (!VerifySession(request.SessionId))
            {
                return await _client.SendPacketAsync(new RoomDeleteResponsePacket(SphynxErrorCode.INVALID_SESSION));
            }

            // Ensure we are deleting a group chat
            var roomType = await SphynxRoomManager.GetRoomFieldAsync<ChatRoomType>(request.RoomId, ChatRoomDbInfo.ROOM_TYPE_FIELD);
            if (!roomType || roomType.Data != ChatRoomType.GROUP)
                return await _client.SendPacketAsync(new RoomDeleteResponsePacket(SphynxErrorCode.INVALID_ROOM));

            // Password check
            var dbPassword = await SphynxRoomManager.GetRoomFieldAsync<string>(request.RoomId, ChatRoomDbInfo.Group.PASSWORD_FIELD);
            var dbSalt = await SphynxRoomManager.GetRoomFieldAsync<string>(request.RoomId, ChatRoomDbInfo.Group.PASSWORD_SALT_FIELD);
            var passwordCheck = PasswordManager.VerifyPassword(dbPassword.Data!, dbSalt.Data!, request.Password ?? string.Empty);

            if (passwordCheck != SphynxErrorCode.SUCCESS) return await _client.SendPacketAsync(new RoomDeleteResponsePacket(passwordCheck));

            // Delete room
            var roomInfo = await SphynxRoomManager.DeleteRoomAsync(request.RoomId);
            if (!roomInfo) return await _client.SendPacketAsync(new RoomDeleteResponsePacket(roomInfo.ErrorCode));

            // Notify participants
            foreach (var userId in roomInfo.Data!.Users)
            {
                if (SphynxClientManager.TryGetClients(userId, out var clients))
                {
                    // Broadcast to all client connections
                    foreach (var client in clients)
                    {
                        // Client will handle retries on failure
                        client.SendPacketAsync(new RoomDeleteBroadcastPacket(request.RoomId)).SafeBackgroundExecute();
                    }
                }
                else
                {
                    // TODO: Somehow find way to make them know they're no longer in it when they log in
                }
            }

            // TODO: delete room and remove users/msgs from it
            return true;
        }

        private bool VerifySession(Guid sessionId)
        {
            return SphynxClientManager.TryGetSessionId(_client, out var actualSession) && sessionId == actualSession;
        }
    }
}