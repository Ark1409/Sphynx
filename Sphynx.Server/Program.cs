using System.Net;
using System.Net.Sockets;

namespace Sphynx.Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SphynxApp.Run(args);

            //string hostName = Dns.GetHostName();
            //var entry = Dns.GetHostEntry(hostName);
            //var entries = Dns.GetHostEntry("127.0.0.1");
            //Console.WriteLine(hostName);

            //Console.WriteLine("------------");

            //for (int i = 0; i < entry.Aliases.Length; i++) Console.WriteLine("Alias: " +  entry.Aliases[i]);
            //for (int i = 0; i < entry.AddressList.Length; i++) Console.WriteLine("Entry: " + entry.AddressList[i]);

            // Act as mid-point
            // Broadcast messages to others within gc (store people within player's gc, or send as list)
            // There exists one public chat (like #osu)
            // You can DM someone directly
            // You can see who is online
            // 
        }
    }
}