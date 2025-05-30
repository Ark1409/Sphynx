// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using MongoDB.Bson.Serialization.Attributes;
using Sphynx.Core;
using Sphynx.ModelV2.User;

namespace Sphynx.ServerV2.Persistence.User
{
    [BsonIgnoreExtraElements]
    public class LastReadMessageInfo : ModelV2.User.LastReadMessageInfo
    {
        private readonly IDictionary<SnowflakeId, SnowflakeId> _lastReadMessages;

        public LastReadMessageInfo() : this(new Dictionary<SnowflakeId, SnowflakeId>())
        {
        }

        public LastReadMessageInfo(IDictionary<SnowflakeId, SnowflakeId> lastReadMessages)
        {
            _lastReadMessages = lastReadMessages;
        }
    }
}
