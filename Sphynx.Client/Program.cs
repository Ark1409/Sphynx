using Sphynx.Client.Core;

namespace Sphynx.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            SphynxClient c  = new SphynxClient();
            c.Start();
        }
    }
}