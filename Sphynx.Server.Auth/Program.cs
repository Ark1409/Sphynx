using System.Runtime.Loader;

namespace Sphynx.Server.Auth
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await using var server = new SphynxAuthServer();

            // ReSharper disable DisposeOnUsingVariable, AccessToDisposedClosure
            AppDomain.CurrentDomain.ProcessExit += (_, _) => server.DisposeAsync().AsTask().Wait();
            AssemblyLoadContext.Default.Unloading += (_) => server.DisposeAsync().AsTask().Wait();
            Console.CancelKeyPress += (_, _) => server.DisposeAsync().AsTask().Wait();

            await server.StartAsync();
        }
    }
}
