// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Sphynx.Core;

namespace Sphynx.Server.Persistence
{
    public class SnowflakeIdSerializer : SerializerBase<SnowflakeId>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, SnowflakeId value)
        {
            context.Writer.WriteBytes(value.ToByteArray());
        }

        public override SnowflakeId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return new SnowflakeId(context.Reader.ReadBytes());
        }
    }
}
