using Sphynx.Core;
using Sphynx.Network.Packet.Response;

namespace Sphynx.Network.Packet.Request
{
    public class KickMemberRequest : SphynxRequest<KickMemberResponse>, IEquatable<KickMemberRequest>
    {
        /// <summary>
        /// Room ID of the room to kick the user from.
        /// </summary>
        public Guid RoomId { get; set; }

        /// <summary>
        /// User ID of the user to kick from the room.
        /// </summary>
        public Guid MemberId { get; set; }

        /// <inheritdoc/>
        public override SphynxRequestType RequestType => SphynxRequestType.KICK_MEMBER_REQ;

        public KickMemberRequest()
        {
        }

        public KickMemberRequest(Guid roomId, Guid memberId)
        {
            RoomId = roomId;
            MemberId = memberId;
        }

        /// <inheritdoc/>
        public bool Equals(KickMemberRequest? other) => base.Equals(other) && RoomId == other?.RoomId && MemberId == other?.MemberId;

        public override KickMemberResponse CreateResponse(SphynxErrorInfo errorInfo) => new KickMemberResponse(errorInfo);
    }
}
