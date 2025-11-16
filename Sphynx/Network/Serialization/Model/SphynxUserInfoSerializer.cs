// Copyright (c) Ark -α- & Specyy. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Sphynx.Core;
using Sphynx.Model.User;

namespace Sphynx.Network.Serialization.Model
{
    public class SphynxUserInfoSerializer : TypeSerializer<SphynxUserInfo>
    {
        public override void Serialize(SphynxUserInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(model.UserId);
            serializer.WriteString(model.UserName);
            serializer.WriteEnum(model.UserStatus);
            serializer.WriteDateTimeOffset(model.CreatedAt);
            serializer.WriteDateTimeOffset(model.LastLogin);
        }

        public override SphynxUserInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadGuid();
            string userName = deserializer.ReadString()!;
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();
            var createdAt = deserializer.ReadDateTimeOffset();
            var lastLogin = deserializer.ReadDateTimeOffset();

            return new SphynxUserInfo(userId, userName, userStatus, createdAt, lastLogin);
        }
    }

    public class SphynxSelfInfoSerializer : TypeSerializer<SphynxSelfInfo>
    {
        public override void Serialize(SphynxSelfInfo model, ref BinarySerializer serializer)
        {
            serializer.WriteGuid(model.UserId);
            serializer.WriteString(model.UserName);
            serializer.WriteEnum(model.UserStatus);
            serializer.WriteDateTimeOffset(model.CreatedAt);
            serializer.WriteDateTimeOffset(model.LastLogin);
        }

        public override SphynxSelfInfo Deserialize(ref BinaryDeserializer deserializer)
        {
            var userId = deserializer.ReadGuid();
            string userName = deserializer.ReadString()!;
            var userStatus = deserializer.ReadEnum<SphynxUserStatus>();
            var createdAt = deserializer.ReadDateTimeOffset();
            var lastLogin = deserializer.ReadDateTimeOffset();

            return new SphynxSelfInfo
            {
                UserId = userId,
                UserName = userName,
                UserStatus = userStatus,
                CreatedAt = createdAt,
                LastLogin = lastLogin,
            };
        }
    }
}
