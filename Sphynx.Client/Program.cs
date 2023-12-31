using Sphynx.Client.Core;

namespace Sphynx.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new SphynxClient(args).Start();
        }
    }
}