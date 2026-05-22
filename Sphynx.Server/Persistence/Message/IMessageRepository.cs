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
        event Action<SphynxMessageInfo>? MessagePosted;
        event Action<SphynxMessageInfo>? MessageDeleted;

        Task<SphynxErrorInfo<SphynxMessageInfo?>> InsertMessageAsync(SphynxMessageInfo message, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo> UpdateMessageAsync(SphynxMessageInfo updatedMessage, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxMessageInfo?>> GetMessageAsync(SnowflakeId roomId, SnowflakeId messageId, CancellationToken cancellationToken = default);

        //
        Task<SphynxErrorInfo<SphynxMessageInfo[]?>> GetMessagesAsync(SnowflakeId roomId,
            SnowflakeId startMessageId,
            int count,
            bool inclusive = true,
            CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxMessageInfo[]?>> GetMessagesAsync(SnowflakeId roomId, int count, CancellationToken cancellationToken = default)
            => GetMessagesAsync(roomId, SnowflakeId.Empty, count, true, cancellationToken);
    }
}
