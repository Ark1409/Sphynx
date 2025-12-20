// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Driver;
using Sphynx.Core;
using Sphynx.Server.Chat.Model;

namespace Sphynx.Server.Chat.Persistence.Message
{
    /// <summary>
    /// A MongoDB data storage backend implementation for <see cref="SphynxChatMessage"/>s.
    /// </summary>
    /// <remarks>
    /// The current implementation assumes that the <see cref="IMongoCollection{SphynxDbChatMessage}">collection</see>
    /// provided during <see cref="MongoMessageRepository(IMongoCollection{SphynxDbChatMessage})">construction</see> is a
    /// <see href="https://www.mongodb.com/docs/manual/core/timeseries-collections/">time-series collection</see> with the
    /// format:
    /// <code>
    /// timeseries: {
    ///     timeField: "created_at",
    ///     metaField: "room_id",
    ///     granularity: "seconds"
    /// }
    /// </code>
    /// and structures its queries around this assumption respectively.
    /// </remarks>
    public class MongoMessageRepository : IMessageRepository
    {
        private readonly IMongoCollection<SphynxDbChatMessage> _msgCollection;

        /// <summary>
        /// Creates a new MongoDB data storage backend implementation for <see cref="SphynxChatMessage"/>s.
        /// </summary>
        /// <remarks>
        /// The current implementation assumes that the <see cref="IMongoCollection{SphynxDbChatMessage}">collection</see>
        /// provided during construction is a
        /// <see href="https://www.mongodb.com/docs/manual/core/timeseries-collections/">time-series collection</see> with the
        /// format:
        /// <code>
        /// timeseries: {
        ///     timeField: "created_at",
        ///     metaField: "room_id",
        ///     granularity: "seconds"
        /// }
        /// </code>
        /// and structures its queries around this assumption respectively.
        /// </remarks>
        public MongoMessageRepository(IMongoDatabase db, string collectionName, bool createIfNotExists = false)
            : this(db.GetCollection<SphynxDbChatMessage>(collectionName))
        {
            if (createIfNotExists)
            {
                db.CreateCollection(collectionName, new CreateCollectionOptions
                    {
                        TimeSeriesOptions = new TimeSeriesOptions(
                            timeField: "created_at",
                            metaField: "room_id",
                            granularity: TimeSeriesGranularity.Seconds
                        )
                    }
                );
            }

            _msgCollection = db.GetCollection<SphynxDbChatMessage>(collectionName);
        }

        /// <summary>
        /// Creates a new MongoDB data storage backend implementation for <see cref="SphynxChatMessage"/>s.
        /// </summary>
        /// <remarks>
        /// The current implementation assumes that the <see cref="IMongoCollection{SphynxDbChatMessage}">collection</see>
        /// provided during construction is a
        /// <see href="https://www.mongodb.com/docs/manual/core/timeseries-collections/">time-series collection</see> with the
        /// format:
        /// <code>
        /// timeseries: {
        ///     timeField: "created_at",
        ///     metaField: "room_id",
        ///     granularity: "seconds"
        /// }
        /// </code>
        /// and structures its queries around this assumption respectively.
        /// </remarks>
        public MongoMessageRepository(IMongoCollection<SphynxDbChatMessage> msgCollection)
        {
            _msgCollection = msgCollection;
        }

        public async Task<SphynxErrorInfo<SphynxChatMessage?>> InsertMessageAsync(SphynxChatMessage msg,
            CancellationToken cancellationToken = default)
        {
            if (msg.SenderId == default)
                return SphynxErrorCode.INVALID_USER;

            if (msg.RoomId == default)
                return SphynxErrorCode.INVALID_ROOM;

            if (msg.MessageId == default)
                msg.MessageId = SnowflakeId.NewId();

            msg.CreatedAt = msg.MessageId.DateTime;
            msg.Content ??= string.Empty;

            var dbMsg = msg.ToRecord();

            await _msgCollection.InsertOneAsync(dbMsg, cancellationToken: cancellationToken).ConfigureAwait(false);
            return msg;
        }

        public async Task<SphynxErrorInfo<SphynxChatMessage?>> GetMessageAsync(Guid? roomId, SnowflakeId msgId,
            CancellationToken cancellationToken = default)
        {
            var filter = GetMessageFilter(roomId, msgId);

            if (filter == null)
                return SphynxErrorCode.INVALID_MSG;

            var dbMsg = await _msgCollection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            return dbMsg is null ? SphynxErrorCode.INVALID_MSG : dbMsg.ToDomain();
        }

        public async Task<SphynxErrorInfo<SphynxChatMessage[]?>> GetMessagesAsync(Guid?[] roomIds, SnowflakeId[] msgIds,
            CancellationToken cancellationToken = default)
        {
            if (roomIds.Length != msgIds.Length)
                return new SphynxErrorInfo<SphynxChatMessage[]?>(SphynxErrorCode.INVALID_ROOM,
                    $"Insufficient {(roomIds.Length > msgIds.Length ? "rooms" : "messages")} specified");

            var msgFilters = new List<FilterDefinition<SphynxDbChatMessage>>(msgIds.Length);

            for (int i = 0; i < msgIds.Length; i++)
            {
                var msgId = msgIds[i];
                var roomId = roomIds[i];

                var filter = GetMessageFilter(roomId, msgId);

                if (filter != null)
                    msgFilters.Add(filter);
            }

            var finalFilter = Builders<SphynxDbChatMessage>.Filter.Or(msgFilters);
            var dbMsgs = await _msgCollection.Find(finalFilter).ToListAsync(cancellationToken).ConfigureAwait(false);

            return dbMsgs.Count <= 0 ? SphynxErrorCode.INVALID_MSG : dbMsgs.Select(x => x.ToDomain()).ToArray();
        }

        public async Task<SphynxErrorInfo<SphynxChatMessage[]?>> GetMessagesAsync(Guid roomId, SnowflakeId startMessageId, int count,
            bool forward = true,
            bool includeStartMessage = true,
            CancellationToken cancellationToken = default)
        {
            if (roomId == default)
                return SphynxErrorCode.INVALID_ROOM;

            var roomFilter = Builders<SphynxDbChatMessage>.Filter.Eq(x => x.RoomId, roomId);
            var timeFilter = GetTimeFilter(startMessageId, forward, includeStartMessage);

            var timeSort = forward
                ? Builders<SphynxDbChatMessage>.Sort.Ascending(x => x.CreatedAt)
                : Builders<SphynxDbChatMessage>.Sort.Descending(x => x.CreatedAt);
            var idSort = forward
                ? Builders<SphynxDbChatMessage>.Sort.Ascending(x => x.MessageId)
                : Builders<SphynxDbChatMessage>.Sort.Descending(x => x.MessageId);

            var filter = Builders<SphynxDbChatMessage>.Filter.And(roomFilter, timeFilter);
            var sort = Builders<SphynxDbChatMessage>.Sort.Combine(timeSort, idSort);

            var dbMsgs = await _msgCollection.Find(filter).Sort(sort).Limit(count).ToListAsync(cancellationToken).ConfigureAwait(false);
            return dbMsgs.Select(x => x.ToDomain()).ToArray();
        }

        public async Task<SphynxErrorInfo<SphynxChatMessage?>> EditMessageAsync(Guid? roomId, SnowflakeId msgId, string newContent,
            CancellationToken cancellationToken = default)
        {
            var filter = GetMessageFilter(roomId, msgId);

            if (filter is null)
                return SphynxErrorCode.INVALID_MSG;

            newContent ??= string.Empty;

            var contentUpdate = Builders<SphynxDbChatMessage>.Update.Set(x => x.Content, newContent);
            var editUpdate = Builders<SphynxDbChatMessage>.Update.Set(x => x.EditedAt, DateTimeOffset.UtcNow);
            var msgUpdate = Builders<SphynxDbChatMessage>.Update.Combine(contentUpdate, editUpdate);

            var oldDbMsg = await _msgCollection.FindOneAndUpdateAsync(filter, msgUpdate, cancellationToken: cancellationToken).ConfigureAwait(false);
            return oldDbMsg is null ? SphynxErrorCode.INVALID_MSG : oldDbMsg.ToDomain();
        }

        public async Task<SphynxErrorInfo> DeleteMessageAsync(Guid? roomId, SnowflakeId msgId, CancellationToken cancellationToken = default)
        {
            var filter = GetMessageFilter(roomId, msgId);

            if (filter is null)
                return SphynxErrorCode.INVALID_MSG;

            var deleteResult = await _msgCollection.DeleteOneAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
            return deleteResult.IsAcknowledged && deleteResult.DeletedCount >= 1 ? SphynxErrorCode.SUCCESS : SphynxErrorCode.INVALID_MSG;
        }

        public async Task<SphynxErrorInfo<long>> DeleteMessagesAsync(Guid?[] roomIds, SnowflakeId[] msgIds,
            CancellationToken cancellationToken = default)
        {
            if (roomIds.Length != msgIds.Length)
                return new SphynxErrorInfo<long>(SphynxErrorCode.INVALID_ROOM,
                    $"Insufficient {(roomIds.Length > msgIds.Length ? "rooms" : "messages")} specified");

            var msgFilters = new List<FilterDefinition<SphynxDbChatMessage>>(msgIds.Length);

            for (int i = 0; i < msgIds.Length; i++)
            {
                var msgId = msgIds[i];
                var roomId = roomIds[i];

                var filter = GetMessageFilter(roomId, msgId);

                if (filter != null)
                    msgFilters.Add(filter);
            }

            var finalFilter = Builders<SphynxDbChatMessage>.Filter.Or(msgFilters);
            var deleteResult = await _msgCollection.DeleteManyAsync(finalFilter, cancellationToken: cancellationToken).ConfigureAwait(false);

            return deleteResult.IsAcknowledged ? deleteResult.DeletedCount : SphynxErrorCode.DB_WRITE_ERROR;
        }

        public async Task<SphynxErrorInfo<long>> DeleteMessagesAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            if (roomId == default)
                return SphynxErrorCode.INVALID_ROOM;

            var deleteFilter = Builders<SphynxDbChatMessage>.Filter.Eq(x => x.RoomId, roomId);
            var deleteResult = await _msgCollection.DeleteManyAsync(deleteFilter, cancellationToken: cancellationToken).ConfigureAwait(false);
            return deleteResult.IsAcknowledged ? deleteResult.DeletedCount : SphynxErrorCode.DB_WRITE_ERROR;
        }

        private FilterDefinition<SphynxDbChatMessage>? GetMessageFilter(Guid? roomId, SnowflakeId msgId)
        {
            if (msgId == default)
                return null;

            FilterDefinition<SphynxDbChatMessage> filter;

            if (roomId is null)
            {
                var timeFilter = Builders<SphynxDbChatMessage>.Filter.Eq(x => x.CreatedAt, msgId.DateTime);
                var msgFilter = Builders<SphynxDbChatMessage>.Filter.Eq(x => x.MessageId, msgId);

                filter = Builders<SphynxDbChatMessage>.Filter.And(timeFilter, msgFilter);
            }
            else
            {
                if (roomId.Value == default)
                    return null;

                var roomFilter = Builders<SphynxDbChatMessage>.Filter.Eq(x => x.RoomId, roomId.Value);
                var timeFilter = Builders<SphynxDbChatMessage>.Filter.Eq(x => x.CreatedAt, msgId.DateTime);
                var msgFilter = Builders<SphynxDbChatMessage>.Filter.Eq(x => x.MessageId, msgId);

                filter = Builders<SphynxDbChatMessage>.Filter.And(roomFilter, timeFilter, msgFilter);
            }

            return filter;
        }

        private static FilterDefinition<SphynxDbChatMessage> GetTimeFilter(SnowflakeId startMessageId, bool forward, bool includeStartMessage)
        {
            FilterDefinition<SphynxDbChatMessage> timeFilter;

            if (forward)
            {
                timeFilter = includeStartMessage
                    ? Builders<SphynxDbChatMessage>.Filter.Gte(x => x.CreatedAt, startMessageId.DateTime)
                    : Builders<SphynxDbChatMessage>.Filter.Gt(x => x.CreatedAt, startMessageId.DateTime);
            }
            else
            {
                timeFilter = includeStartMessage
                    ? Builders<SphynxDbChatMessage>.Filter.Lte(x => x.CreatedAt, startMessageId.DateTime)
                    : Builders<SphynxDbChatMessage>.Filter.Lt(x => x.CreatedAt, startMessageId.DateTime);
            }

            return timeFilter;
        }
    }
}
