// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model;

namespace Sphynx.Server.Persistence.Message
{
    /// <summary>
    /// Represents a storage space for chat messages handled by the server.
    /// </summary>
    /// <remarks>Unless explicitly stated otherwise, all tasks will complete successfully for validation-related issues, the tasks complete successfully, but there may be sphynx error</remarks>
    public interface IMessageRepository
    {
        event Action<SphynxChatMessage>? MessagePosted;
        event Action<SphynxChatMessage>? MessageDeleted;

        Task<SphynxErrorInfo<SphynxChatMessage?>> InsertMessageAsync(SphynxChatMessage message, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo> UpdateMessageAsync(SphynxChatMessage updatedMessage, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatMessage?>> GetMessageAsync(SnowflakeId roomId, SnowflakeId messageId, CancellationToken cancellationToken = default);

        //
        Task<SphynxErrorInfo<SphynxChatMessage[]?>> GetMessagesAsync(SnowflakeId roomId,
            SnowflakeId startMessageId,
            int count,
            bool inclusive = true,
            CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatMessage[]?>> GetMessagesAsync(SnowflakeId roomId, int count, CancellationToken cancellationToken = default)
            => GetMessagesAsync(roomId, SnowflakeId.Empty, count, true, cancellationToken);
    }
}
