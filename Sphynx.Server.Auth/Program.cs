using System.Net;
using System.Xml.Schema;
using Sphynx.Network.PacketV2;
using Sphynx.Network.Serialization.Model;
using Sphynx.Network.Serialization.Packet;
using Sphynx.Network.Transport;
using Sphynx.ServerV2;

namespace Sphynx.Server.Auth
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var transporter = new PacketTransporter();

            transporter.AddSerializer(SphynxPacketType.LOGIN_REQ, new LoginRequestPacketSerializer());
            transporter.AddSerializer(SphynxPacketType.LOGIN_RES, new LoginResponseSerializer(new SphynxSelfInfoSerializer()));
            transporter.AddSerializer(SphynxPacketType.REGISTER_REQ, new RegisterRequestSerializer());
            transporter.AddSerializer(SphynxPacketType.REGISTER_RES, new RegisterResponseSerializer(new SphynxSelfInfoSerializer()));

            var profile = new SphynxTcpServerProfile
            {
                PacketTransporter = transporter,
            };

            await using var server = new SphynxAuthServer(profile);
            await server.StartAsync();
        }
    }
}
