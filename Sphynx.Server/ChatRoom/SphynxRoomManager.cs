using Sphynx.Model.ChatRoom;
using Sphynx.Network.Packet;
using Sphynx.Server.Storage;
using Sphynx.Server.Utils;
using Sphynx.Utils;

namespace Sphynx.Server.ChatRoom
{
    public delegate void MessageEdited(Guid msgId, string oldContent, string newContent);

    public static class SphynxRoomManager
    {
        private static readonly DatabaseStore<Guid, ChatRoomDbInfo.Direct> _directRoomStore;
        private static readonly DatabaseStore<Guid, ChatRoomDbInfo.Group> _groupRoomStore;
        // TODO: Why not merge these into one?
        private static readonly DatabaseStore<Guid, ChatRoomMessageDbInfo> _directMessageStore;
        private static readonly DatabaseStore<Guid, ChatRoomMessageDbInfo> _groupMessageStore;

        public static event Action<ChatRoomInfo>? ChatRoomCreated;
        public static event Action<ChatRoomInfo>? ChatRoomDeleted;
        public static event Action<ChatRoomMessageDbInfo>? MessageAdded;
        public static event Action<ChatRoomMessageDbInfo>? MessageDeleted;
        public static event MessageEdited? MessageEdited;

        static SphynxRoomManager()
        {
            using (var reader = new StreamReader(File.OpenRead(DatabaseInfoFile.NAME)))
            {
                reader.ReadLine();
                reader.ReadLine();
                reader.ReadLine();
                string roomCollectionName = reader.ReadLine()!;
                string directCollectionName = reader.ReadLine()!;
                string groupCollectionName = reader.ReadLine()!;

                _directRoomStore = new MongoStore<ChatRoomDbInfo.Direct>(roomCollectionName);
                _groupRoomStore = new MongoStore<ChatRoomDbInfo.Group>(roomCollectionName);
                _directMessageStore = new MongoStore<ChatRoomMessageDbInfo>(directCollectionName);
                _groupMessageStore = new MongoStore<ChatRoomMessageDbInfo>(groupCollectionName);
            }
        }

        public static async Task<SphynxErrorInfo<ChatRoomDbInfo.Direct?>> CreateDirectRoomAsync(Guid userOne, Guid userTwo)
        {
            // Ensure DM has not already been created with these users
            // TODO: somehow embed DM users in roomID perhaps? (preferably, we would like them indexed...)
            if (await _directRoomStore.ContainsFieldAsync(ChatRoomDbInfo.USERS_FIELD, new[] { userOne, userTwo })
                || await _directRoomStore.ContainsFieldAsync(ChatRoomDbInfo.USERS_FIELD, new[] { userTwo, userOne }))
            {
                return new SphynxErrorInfo<ChatRoomDbInfo.Direct?>(SphynxErrorCode.INVALID_ROOM);
            }

            // TODO: Decide on convention for DM room name
            string roomName = string.Join(userOne.ToString(), userTwo.ToString());
            var newRoom = new ChatRoomDbInfo.Direct(roomName, userOne, userTwo);

            if (await _directRoomStore.InsertAsync(newRoom))
            {
                ChatRoomCreated?.Invoke(newRoom);
                return new SphynxErrorInfo<ChatRoomDbInfo.Direct?>(newRoom);
            }

            return new SphynxErrorInfo<ChatRoomDbInfo.Direct?>(SphynxErrorCode.DB_WRITE_ERROR);
        }

        public static async Task<SphynxErrorInfo<ChatRoomDbInfo.Group?>> 
            CreateGroupRoomAsync(Guid ownerId, string name, bool @public = true,
            string? password = null)
        {
            ChatRoomDbInfo.Group newRoom;
            if (string.IsNullOrEmpty(password))
            {
                newRoom = new ChatRoomDbInfo.Group(ownerId, name, @public);
            }
            else
            {
                byte[] pwd = PasswordManager.HashPassword(password, out byte[] pwdSalt);
                newRoom = new ChatRoomDbInfo.Group(ownerId, name, pwd, pwdSalt, @public);
            }

            if (await _groupRoomStore.InsertAsync(newRoom))
            {
                // Immediately null-out password
                newRoom.Password = null;
                newRoom.PasswordSalt = null;

                ChatRoomCreated?.Invoke(newRoom);
                return new SphynxErrorInfo<ChatRoomDbInfo.Group?>(SphynxErrorCode.SUCCESS);
            }

            return new SphynxErrorInfo<ChatRoomDbInfo.Group?>(SphynxErrorCode.DB_WRITE_ERROR);
        }

        public static Task<SphynxErrorInfo<ChatRoomMessageDbInfo>> AddMessageAsync(Guid roomId, Guid senderId, string content)
        {
            return AddMessageAsync(roomId, senderId, DateTimeOffset.Now, content);
        }

        public static async Task<SphynxErrorInfo<ChatRoomMessageDbInfo>> AddMessageAsync(Guid roomId, Guid senderId, DateTimeOffset timestamp,
            string content)
        {
            var roomType = await GetRoomFieldAsync<ChatRoomType>(roomId, ChatRoomDbInfo.ROOM_TYPE_FIELD);
            if (roomType.ErrorCode != SphynxErrorCode.SUCCESS) return new SphynxErrorInfo<ChatRoomMessageDbInfo>(SphynxErrorCode.INVALID_ROOM);

            var newMsg = new ChatRoomMessageDbInfo(roomId, senderId, timestamp, content ??= string.Empty);

            switch (roomType.Data)
            {
                case ChatRoomType.DIRECT_MSG:
                    if (await _directMessageStore.InsertAsync(newMsg))
                    {
                        MessageAdded?.Invoke(newMsg);
                        return new SphynxErrorInfo<ChatRoomMessageDbInfo>(newMsg);
                    }

                    break;
                case ChatRoomType.GROUP:
                    if (await _groupMessageStore.InsertAsync(newMsg))
                    {
                        MessageAdded?.Invoke(newMsg);
                        return new SphynxErrorInfo<ChatRoomMessageDbInfo>(newMsg);
                    }

                    break;
            }

            return new SphynxErrorInfo<ChatRoomMessageDbInfo>(SphynxErrorCode.DB_WRITE_ERROR);
        }

        /// <summary>
        /// Edits a pre-existing message.
        /// </summary>
        /// <param name="msgId">The id of the message to edit.</param>
        /// <param name="newContent">The new message content.</param>
        /// <returns>The old message content.</returns>
        public static Task<SphynxErrorInfo<string?>> EditMessageAsync(Guid msgId, string newContent)
        {
            return EditMessageAsync(msgId, DateTimeOffset.UtcNow, newContent);
        }

        /// <summary>
        /// Edits a pre-existing message.
        /// </summary>
        /// <param name="msgId">The id of the message to edit.</param>
        /// <param name="editTimestamp">The timestamp at which this message was edited.</param>
        /// <param name="newContent">The new message content.</param>
        /// <returns>The old message content.</returns>
        public static async Task<SphynxErrorInfo<string?>> EditMessageAsync(Guid msgId, DateTimeOffset editTimestamp, string newContent)
        {
            var roomId = await GetMessageFieldAsync<Guid>(msgId, ChatRoomMessageDbInfo.ROOM_ID_FIELD);
            if (roomId.ErrorCode != SphynxErrorCode.SUCCESS) return new SphynxErrorInfo<string?>(roomId.ErrorCode);

            var oldContent = await GetMessageFieldAsync<string>(msgId, ChatRoomMessageDbInfo.CONTENT_FIELD);
            if (oldContent.ErrorCode != SphynxErrorCode.SUCCESS) oldContent.Data = null!;

            var roomType = await GetRoomFieldAsync<ChatRoomType>(roomId.Data, ChatRoomDbInfo.ROOM_TYPE_FIELD);

            newContent ??= string.Empty;

            switch (roomType.Data)
            {
                case ChatRoomType.DIRECT_MSG:
                    if (await _directMessageStore.PutFieldAsync(msgId, ChatRoomMessageDbInfo.CONTENT_FIELD, newContent))
                    {
                        await _directMessageStore.PutFieldAsync(msgId, ChatRoomMessageDbInfo.EDITED_TIMESTAMP_FIELD, editTimestamp);
                        MessageEdited?.Invoke(msgId, oldContent.Data!, newContent);
                        return oldContent;
                    }

                    break;
                case ChatRoomType.GROUP:
                    if (await _groupMessageStore.PutFieldAsync(msgId, ChatRoomMessageDbInfo.CONTENT_FIELD, newContent))
                    {
                        await _directMessageStore.PutFieldAsync(msgId, ChatRoomMessageDbInfo.EDITED_TIMESTAMP_FIELD, editTimestamp);
                        MessageEdited?.Invoke(msgId, oldContent.Data!, newContent);
                        return oldContent;
                    }

                    break;
            }

            return new SphynxErrorInfo<string?>(SphynxErrorCode.DB_READ_ERROR);
        }

        public static async Task<SphynxErrorInfo<ChatRoomMessageDbInfo?>> DeleteMessageAsync(Guid msgId)
        {
            var msgInfo = await GetMessageAsync(msgId);
            if (msgInfo.ErrorCode != SphynxErrorCode.SUCCESS) return new SphynxErrorInfo<ChatRoomMessageDbInfo?>(msgInfo.ErrorCode);

            var roomType = await GetRoomFieldAsync<ChatRoomType>(msgInfo.Data!.RoomId, ChatRoomDbInfo.ROOM_TYPE_FIELD);

            switch (roomType.Data!)
            {
                case ChatRoomType.DIRECT_MSG:
                    if (await _directMessageStore.DeleteAsync(msgId))
                    {
                        MessageDeleted?.Invoke(msgInfo.Data);
                        return msgInfo;
                    }

                    break;

                case ChatRoomType.GROUP:
                    if (await _groupMessageStore.DeleteAsync(msgId))
                    {
                        MessageDeleted?.Invoke(msgInfo.Data);
                        return msgInfo;
                    }

                    break;
            }

            return new SphynxErrorInfo<ChatRoomMessageDbInfo?>(SphynxErrorCode.INVALID_MSG);
        }

        public static async Task<SphynxErrorInfo<ChatRoomMessageDbInfo?>> GetMessageAsync(Guid msgId)
        {
            var directMsg = await _directMessageStore.GetAsync(msgId);

            if (directMsg.ErrorCode == SphynxErrorCode.SUCCESS)
                return directMsg;

            var groupMsg =
                await _groupMessageStore.GetAsync(msgId, ChatRoomDbInfo.Group.PASSWORD_FIELD, ChatRoomDbInfo.Group.PASSWORD_SALT_FIELD);

            return groupMsg.ErrorCode == SphynxErrorCode.SUCCESS
                ? groupMsg
                : new SphynxErrorInfo<ChatRoomMessageDbInfo?>(SphynxErrorCode.INVALID_MSG);
        }

        public static async Task<SphynxErrorInfo<T?>> GetMessageFieldAsync<T>(Guid msgId, string fieldName)
        {
            var field = await _directMessageStore.GetFieldAsync<T>(msgId, fieldName);
            return field.ErrorCode == SphynxErrorCode.SUCCESS
                ? new SphynxErrorInfo<T?>(field.Data)
                : new SphynxErrorInfo<T?>((await _groupMessageStore.GetFieldAsync<T>(msgId, fieldName)).Data);
        }

        public static async Task<SphynxErrorInfo<ChatRoomInfo?>> GetRoomAsync(Guid roomId)
        {
            var roomInfo = await _directRoomStore.GetAsync(roomId);
            return roomInfo.ErrorCode == SphynxErrorCode.SUCCESS
                ? new SphynxErrorInfo<ChatRoomInfo?>(roomInfo.Data)
                : new SphynxErrorInfo<ChatRoomInfo?>((await _groupRoomStore.GetAsync(roomId, ChatRoomDbInfo.Group.PASSWORD_FIELD,
                    ChatRoomDbInfo.Group.PASSWORD_SALT_FIELD)).Data);
        }

        public static async Task<SphynxErrorInfo<ChatRoomDbInfo.Group?>> GetGroupRoomAsync(Guid roomId, bool includePassword = false)
        {
            return includePassword
                ? new SphynxErrorInfo<ChatRoomDbInfo.Group?>((await _groupRoomStore.GetAsync(roomId)).Data)
                : new SphynxErrorInfo<ChatRoomDbInfo.Group?>((await _groupRoomStore.GetAsync(roomId, ChatRoomDbInfo.Group.PASSWORD_FIELD,
                    ChatRoomDbInfo.Group.PASSWORD_SALT_FIELD)).Data);
        }

        public static async Task<SphynxErrorInfo<T?>> GetRoomFieldAsync<T>(Guid roomId, string fieldName)
        {
            var field = await _directRoomStore.GetFieldAsync<T>(roomId, fieldName);
            return field.ErrorCode == SphynxErrorCode.SUCCESS
                ? new SphynxErrorInfo<T?>(field.Data)
                : new SphynxErrorInfo<T?>((await _groupRoomStore.GetFieldAsync<T>(roomId, fieldName)).Data);
        }

        public static async Task<bool> UpdateRoomFieldAsync<T>(Guid roomId, string fieldName, T? value)
        {
            return await _directRoomStore.ContainsAsync(roomId)
                ? await _directRoomStore.PutFieldAsync(roomId, fieldName, value)
                : await _groupRoomStore.PutFieldAsync(roomId, fieldName, value);
        }

        public static async Task<SphynxErrorInfo<ChatRoomInfo?>> DeleteRoomAsync(Guid roomId)
        {
            var roomInfo = await GetRoomAsync(roomId);

            if (roomInfo.ErrorCode != SphynxErrorCode.SUCCESS)
            {
                return new SphynxErrorInfo<ChatRoomInfo?>(SphynxErrorCode.INVALID_ROOM);
            }

            // Delete room
            bool deletedRoom = roomInfo.Data!.RoomType == ChatRoomType.DIRECT_MSG
                ? await _directRoomStore.DeleteAsync(roomId)
                : await _groupRoomStore.DeleteAsync(roomId);

            if (!deletedRoom)
                return new SphynxErrorInfo<ChatRoomInfo?>(SphynxErrorCode.DB_WRITE_ERROR);

            // Delete messages
            switch (roomInfo.Data!.RoomType)
            {
                case ChatRoomType.DIRECT_MSG:
                    // There might be zero messages - ignore return value
                    await _directMessageStore.DeleteWhereAsync(ChatRoomMessageDbInfo.ROOM_ID_FIELD, roomInfo.Data!.RoomId);
                    break;
                case ChatRoomType.GROUP:
                    // There might be zero messages - ignore return value
                    await _groupMessageStore.DeleteWhereAsync(ChatRoomMessageDbInfo.ROOM_ID_FIELD, roomInfo.Data!.RoomId);
                    break;
            }

            ChatRoomDeleted?.Invoke(roomInfo.Data!);
            return roomInfo;
        }
    }
}