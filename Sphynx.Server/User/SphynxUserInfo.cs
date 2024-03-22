using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Sphynx.Core;
using Sphynx.Server.Storage;

namespace Sphynx.Server.User
{
    /// <summary>
    /// Represents a <see cref="ISphynxUserInfo"/> document for a MongoDB database.
    /// </summary>
    [BsonIgnoreExtraElements]
    public class SphynxUserInfo : ISphynxUserInfo, IEquatable<SphynxUserInfo>, IIdentifiable<Guid>
    {
        /// <inheritdoc/>
        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid UserId { get; internal set; }

        /// <inheritdoc/>
        [BsonIgnore]
        public Guid Id => UserId;

        /// <inheritdoc/>
        [BsonElement("name")]
        public string UserName { get; internal set; }

        /// <inheritdoc/>
        [BsonElement("status")]
        public SphynxUserStatus UserStatus { get; internal set; }

        /// <summary>
        /// User IDs of friends for this user.
        /// </summary>
        [BsonElement("friends")]
        public HashSet<Guid> Friends { get; internal set; }

        /// <summary>
        /// Creates a new <see cref="SphynxUserInfo"/>.
        /// </summary>
        /// <param name="userId">The user ID of the Sphynx user.</param>
        /// <param name="userName">The username of the Sphynx user.</param>
        /// <param name="status">The activity status of the Sphynx user.</param>
        /// <param name="friends">User IDs of friends for this user.</param>
        public SphynxUserInfo(Guid userId, string userName, SphynxUserStatus status, IEnumerable<Guid>? friends = null)
        {
            UserId = userId;
            UserName = userName;
            UserStatus = status;
            Friends = new HashSet<Guid>();

            if (friends is not null)
            {
                foreach (var friend in friends)
                    Debug.Assert(Friends.Add(friend));
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as ISphynxUserInfo);

        /// <inheritdoc/>
        public bool Equals(SphynxUserInfo? other) => Equals(other as ISphynxUserInfo);

        /// <inheritdoc/>
        public bool Equals(ISphynxUserInfo? other) => UserId == other?.UserId;

        /// <inheritdoc/>
        public override int GetHashCode() => Id.GetHashCode();
    }
}