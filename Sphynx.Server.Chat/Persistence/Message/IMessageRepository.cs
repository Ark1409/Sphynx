// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Server.Chat.Model;

namespace Sphynx.Server.Chat.Persistence.Message
{
    public interface IMessageRepository
    {
        Task<SphynxErrorInfo<SphynxChatMessage?>> InsertMessageAsync(SphynxChatMessage msg, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<SphynxChatMessage?>> GetMessageAsync(Guid? roomId, SnowflakeId msgId, CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatMessage[]?>> GetMessagesAsync(Guid?[] roomIds, SnowflakeId[] msgIds,
            CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatMessage[]?>> GetMessagesAsync(Guid roomId,
            SnowflakeId startMessageId,
            int count,
            bool forward = false,
            bool includeStartMessage = true,
            CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo<SphynxChatMessage?>> EditMessageAsync(Guid? roomId, SnowflakeId msgId, string newContent,
            CancellationToken cancellationToken = default);

        Task<SphynxErrorInfo> DeleteMessageAsync(Guid? roomId, SnowflakeId msgId, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<long>> DeleteMessagesAsync(Guid?[] roomIds, SnowflakeId[] msgIds, CancellationToken cancellationToken = default);
        Task<SphynxErrorInfo<long>> DeleteMessagesAsync(Guid roomId, CancellationToken cancellationToken = default);
    }

    public static class MessageRepositoryExtensions
    {
        public static Task<SphynxErrorInfo<SphynxChatMessage[]?>> GetMessagesAsync(this IMessageRepository repository,
            Guid roomId,
            int count,
            CancellationToken cancellationToken = default)
        {
            return repository.GetMessagesAsync(roomId, SnowflakeId.NewTimestamp(), count, forward: false, includeStartMessage: true,
                cancellationToken);
        }
    }
}
