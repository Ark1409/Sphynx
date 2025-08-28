namespace Sphynx.Server.Auth
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await using var server = new SphynxAuthServer();

            // ReSharper disable DisposeOnUsingVariable, AccessToDisposedClosure
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                server.DisposeAsync().AsTask().Wait();
                eventArgs.Cancel = true;
            };

            await server.StartAsync();
        }
    }
}
